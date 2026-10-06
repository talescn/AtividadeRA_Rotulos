using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

// Busca os dados de um produto pelo GTIN, nesta ordem:
// 1. JSON local em Resources/Produtos/{gtin}.json (transcrito do rotulo);
// 2. cache em Application.persistentDataPath/Produtos/{gtin}.json;
// 3. Open Food Facts (o resultado vai para o cache).
// Sem internet nada quebra: quem chama continua com o dado local ou com a foto da tabela.
public class RepositorioProdutos
{
    public const string PastaRecursos = "Produtos";
    public const string Contato = "https://github.com/talescn/AtividadeRA_Rotulos";
    // So os campos que o app usa
    public const string Campos = "code,product_name,product_name_pt,brands,serving_size,serving_quantity," +
                                 "nutriments,ingredients_text_pt,ingredients_text,last_modified_t";
    const string UrlProduto = "https://world.openfoodfacts.org/api/v2/product/{0}.json?fields={1}";
    const int TempoLimiteSegundos = 10;

    // O Open Food Facts permite 15 leituras de produto por minuto; o limite vale para o app inteiro
    static readonly LimiteRequisicoes limite = new LimiteRequisicoes(15, 60f);

    // Chave do app -> campo do Open Food Facts. O OFF guarda as massas em gramas, dai o fator.
    static readonly (string chave, string campo, string nome, string unidade, float fator, int nivel)[] Mapa =
    {
        ("energia", "energy-kcal", "Valor energético", "kcal", 1f, 0),
        ("carboidratos", "carbohydrates", "Carboidratos", "g", 1f, 0),
        ("acucaresTotais", "sugars", "Açúcares totais", "g", 1f, 1),
        ("acucaresAdicionados", "added-sugars", "Açúcares adicionados", "g", 1f, 2),
        ("proteinas", "proteins", "Proteínas", "g", 1f, 0),
        ("gordurasTotais", "fat", "Gorduras totais", "g", 1f, 0),
        ("gordurasSaturadas", "saturated-fat", "Gorduras saturadas", "g", 1f, 1),
        ("gordurasTrans", "trans-fat", "Gorduras trans", "g", 1f, 1),
        ("fibras", "fiber", "Fibras alimentares", "g", 1f, 0),
        ("sodio", "sodium", "Sódio", "mg", 1000f, 0),
        ("vitaminaA", "vitamin-a", "Vitamina A", "µg", 1000000f, 0),
        ("vitaminaD", "vitamin-d", "Vitamina D", "µg", 1000000f, 0),
        ("vitaminaE", "vitamin-e", "Vitamina E", "mg", 1000f, 0),
        ("vitaminaC", "vitamin-c", "Vitamina C", "mg", 1000f, 0),
        ("calcio", "calcium", "Cálcio", "mg", 1000f, 0),
        ("ferro", "iron", "Ferro", "mg", 1000f, 0),
        ("zinco", "zinc", "Zinco", "mg", 1000f, 0)
    };

    readonly string pastaCache;

    public RepositorioProdutos(string pastaCache)
    {
        this.pastaCache = pastaCache;
    }

    public static RepositorioProdutos Padrao()
    {
        return new RepositorioProdutos(Path.Combine(Application.persistentDataPath, PastaRecursos));
    }

    // Formato pedido pelo Open Food Facts: App/versao (contato)
    public static string UserAgent => $"AtividadeRA_Rotulos/{Application.version} ({Contato})";

    // Passos 1 e 2: nao usam a rede e respondem na hora
    public DadosProduto BuscarSemRede(string gtin)
    {
        if (string.IsNullOrEmpty(gtin)) return null;
        return CarregarLocal(gtin) ?? CarregarCache(gtin);
    }

    public static DadosProduto CarregarLocal(string gtin)
    {
        var arquivo = Resources.Load<TextAsset>(PastaRecursos + "/" + gtin);
        return arquivo != null ? Ler(arquivo.text) : null;
    }

    public DadosProduto CarregarCache(string gtin)
    {
        string caminho = CaminhoCache(gtin);
        if (!File.Exists(caminho)) return null;
        try
        {
            return Ler(File.ReadAllText(caminho));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RA] Cache invalido para {gtin}: {e.Message}");
            return null;
        }
    }

    public void SalvarCache(DadosProduto dados)
    {
        try
        {
            Directory.CreateDirectory(pastaCache);
            File.WriteAllText(CaminhoCache(dados.gtin), Escrever(dados));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RA] Nao foi possivel salvar o cache de {dados.gtin}: {e.Message}");
        }
    }

    // Passo 3, em corrotina. Chama aoTerminar com os dados, ou com null e o motivo, para mostrar ao usuario
    // (sem internet, produto fora da base, base sem tabela, tabela impossivel ou limite de consultas atingido).
    public IEnumerator BuscarOpenFoodFacts(string gtin, Action<DadosProduto, string> aoTerminar)
    {
        if (!limite.TentarRegistrar(Time.realtimeSinceStartup))
        {
            Debug.LogWarning($"[RA] Limite de 15 consultas por minuto ao Open Food Facts; {gtin} fica sem consulta.");
            aoTerminar(null, "muitas consultas seguidas; tente de novo em um minuto");
            yield break;
        }

        using (var req = UnityWebRequest.Get(string.Format(UrlProduto, gtin, Campos)))
        {
            req.SetRequestHeader("User-Agent", UserAgent);
            req.timeout = TempoLimiteSegundos;
            yield return req.SendWebRequest();

            if (req.responseCode == 404)
            {
                Debug.Log($"[RA] {gtin} nao esta no Open Food Facts.");
                aoTerminar(null, "não está no Open Food Facts");
                yield break;
            }
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[RA] Open Food Facts indisponivel para {gtin}: {req.error}");
                aoTerminar(null, "sem conexão com o Open Food Facts");
                yield break;
            }

            DadosProduto dados;
            bool encontrado;
            try
            {
                string texto = req.downloadHandler.text;
                encontrado = Numero(JObject.Parse(texto)["status"]) == 1f;
                dados = ConverterOpenFoodFacts(texto);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RA] Resposta do Open Food Facts nao entendida para {gtin}: {e.Message}");
                aoTerminar(null, "resposta do Open Food Facts não entendida");
                yield break;
            }

            if (dados == null)
            {
                Debug.Log($"[RA] {gtin} {(encontrado ? "esta no Open Food Facts, mas sem tabela nutricional" : "nao esta no Open Food Facts")}.");
                aoTerminar(null, encontrado ? "está no Open Food Facts, mas sem tabela nutricional" : "não está no Open Food Facts");
                yield break;
            }

            // A base e aberta e tem cadastros errados: melhor nao mostrar nada do que uma tabela impossivel
            string problema = MotivoImplausivel(dados);
            if (problema != null)
            {
                Debug.LogWarning($"[RA] Dados do Open Food Facts para {gtin} recusados: {problema}");
                aoTerminar(null, "a tabela do Open Food Facts tem valores impossíveis");
                yield break;
            }

            SalvarCache(dados);
            aoTerminar(dados, null);
        }
    }

    // Resposta da API v2 -> DadosProduto. Devolve null se o produto nao existe ou nao tem tabela.
    // O Open Food Facts nao guarda o %VD, entao ele fica null.
    public static DadosProduto ConverterOpenFoodFacts(string json)
    {
        var raiz = JObject.Parse(json);
        var produto = raiz["product"] as JObject;
        if (Numero(raiz["status"]) != 1f || produto == null) return null;

        var dados = new DadosProduto
        {
            gtin = Texto(raiz["code"]) ?? Texto(produto["code"]),
            nome = Texto(produto["product_name_pt"]) ?? Texto(produto["product_name"]),
            marca = Texto(produto["brands"]),
            conferidoEm = Data(produto["last_modified_t"]),
            fonte = DadosProduto.FonteOpenFoodFacts,
            porcao = Porcao(Texto(produto["serving_size"]), Numero(produto["serving_quantity"])),
            ingredientes = Texto(produto["ingredients_text_pt"]) ?? Texto(produto["ingredients_text"])
        };
        dados.base100 = dados.porcao != null && dados.porcao.unidade == "ml" ? "100 ml" : "100 g";

        var nutrientes = produto["nutriments"] as JObject;
        if (nutrientes != null)
        {
            foreach (var m in Mapa)
            {
                float? por100 = Numero(nutrientes[m.campo + "_100g"]);
                float? porPorcao = Numero(nutrientes[m.campo + "_serving"]);
                if (por100 == null && porPorcao == null) continue;
                dados.nutrientes.Add(new DadosProduto.Nutriente
                {
                    chave = m.chave,
                    nome = m.nome,
                    unidade = m.unidade,
                    por100 = Arredondar(por100 * m.fator),
                    porPorcao = Arredondar(porPorcao * m.fator),
                    nivel = m.nivel
                });
            }
        }
        return dados.TemTabela ? dados : null;
    }

    // Devolve o motivo se a tabela for fisicamente impossivel; null se estiver plausivel.
    // Na tabela do rotulo a tolerancia de energia e 15%; aqui e maior porque a base mistura arredondamentos.
    public static string MotivoImplausivel(DadosProduto dados)
    {
        const float ToleranciaEnergia = 0.3f;
        float? porcao = dados.porcao?.quantidade;
        foreach (var n in dados.nutrientes)
        {
            if (n.unidade != "g") continue;
            if (n.por100 > 100f) return $"{n.nome} com {n.por100} g em 100";
            if (porcao.HasValue && n.porPorcao > porcao.Value) return $"{n.nome} com {n.porPorcao} g numa porcao de {porcao} {dados.porcao.unidade}";
        }

        float? kcal = dados.Buscar("energia")?.por100;
        float? carboidratos = dados.Buscar("carboidratos")?.por100;
        float? proteinas = dados.Buscar("proteinas")?.por100;
        float? gorduras = dados.Buscar("gordurasTotais")?.por100;
        if (kcal > 0f && carboidratos.HasValue && proteinas.HasValue && gorduras.HasValue)
        {
            float calculado = 4f * carboidratos.Value + 4f * proteinas.Value + 9f * gorduras.Value;
            if (Math.Abs(kcal.Value - calculado) / kcal.Value > ToleranciaEnergia)
                return $"{kcal} kcal em 100, mas os macronutrientes dao {calculado:0} kcal";
        }
        return null;
    }

    public static DadosProduto Ler(string json)
    {
        return JsonConvert.DeserializeObject<DadosProduto>(json);
    }

    public static string Escrever(DadosProduto dados)
    {
        return JsonConvert.SerializeObject(dados, Formatting.Indented);
    }

    string CaminhoCache(string gtin)
    {
        return Path.Combine(pastaCache, gtin + ".json");
    }

    // "200 ml", "1 serving (200 ml)" ou "17 g" -> quantidade e unidade
    static DadosProduto.Porcao Porcao(string texto, float? quantidade)
    {
        if (texto == null && quantidade == null) return null;
        var porcao = new DadosProduto.Porcao { descricao = texto, quantidade = quantidade, unidade = "g" };
        var achado = texto != null ? Regex.Match(texto, @"(\d+(?:[.,]\d+)?)\s*(ml|g)\b", RegexOptions.IgnoreCase) : Match.Empty;
        if (achado.Success)
        {
            porcao.unidade = achado.Groups[2].Value.ToLowerInvariant();
            if (porcao.quantidade == null)
                porcao.quantidade = float.Parse(achado.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
        }
        return porcao;
    }

    static string Texto(JToken token)
    {
        if (token == null || token.Type == JTokenType.Null) return null;
        string texto = token.ToString().Trim();
        return texto.Length > 0 ? texto : null;
    }

    // O Open Food Facts as vezes manda numero como texto ("200")
    static float? Numero(JToken token)
    {
        if (token == null) return null;
        if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float) return token.Value<float>();
        if (token.Type == JTokenType.String &&
            float.TryParse(token.Value<string>().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float valor))
            return valor;
        return null;
    }

    static float? Arredondar(float? valor)
    {
        return valor.HasValue ? (float)Math.Round(valor.Value, 2) : (float?)null;
    }

    // Segundos Unix (last_modified_t) -> AAAA-MM-DD. Em float a data poderia escorregar alguns minutos.
    static string Data(JToken segundosUnix)
    {
        if (segundosUnix == null || segundosUnix.Type != JTokenType.Integer) return null;
        return DateTimeOffset.FromUnixTimeSeconds(segundosUnix.Value<long>()).UtcDateTime
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
