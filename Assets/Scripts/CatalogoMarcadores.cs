using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

// Marcadores ensinados no proprio app: a imagem de cada rotulo (PNG) e um indice em JSON, numa pasta do
// aparelho. Cada produto (GTIN) pode ter algumas vistas (frente e lados de uma garrafa); quando passa do
// limite, a vista mais antiga sai.
public class CatalogoMarcadores
{
    public const int VistasPorProduto = 4;
    const string ArquivoIndice = "marcadores.json";

    [Serializable]
    public class Marcador
    {
        public string gtin;
        // Nome do PNG dentro da pasta do catalogo
        public string arquivo;
        public float larguraMetros;
        // Nota da imagem quando foi ensinada (QualidadeMarcador)
        public int estrelas;
        // Hora local do aparelho, AAAA-MM-DDTHH:mm:ss
        public string criadoEm;
    }

    readonly string pasta;
    readonly List<Marcador> marcadores;

    public CatalogoMarcadores(string pasta)
    {
        this.pasta = pasta;
        marcadores = Carregar();
    }

    public static CatalogoMarcadores Padrao()
    {
        return new CatalogoMarcadores(Path.Combine(Application.persistentDataPath, "Marcadores"));
    }

    public IReadOnlyList<Marcador> Marcadores => marcadores;

    // O mesmo instante da o nome do PNG e o do marcador criado no Vuforia
    public static string NomeArquivo(string gtin, DateTime agora)
    {
        return gtin + "_" + agora.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture) + ".png";
    }

    public int ContarVistas(string gtin)
    {
        return marcadores.FindAll(m => m.gtin == gtin).Count;
    }

    // Salva a imagem e a entrada no indice. As vistas mais antigas do produto que sairam para respeitar
    // o limite vao para removidos (o gerenciador tira os marcadores delas da cena).
    public Marcador Adicionar(string gtin, byte[] png, float larguraMetros, int estrelas, DateTime agora,
                              List<Marcador> removidos)
    {
        if (!DadosProduto.GtinValido(gtin)) throw new ArgumentException("GTIN invalido: " + gtin, nameof(gtin));
        if (png == null || png.Length == 0) throw new ArgumentException("Imagem vazia", nameof(png));

        Directory.CreateDirectory(pasta);
        var novo = new Marcador
        {
            gtin = gtin,
            arquivo = NomeArquivo(gtin, agora),
            larguraMetros = larguraMetros,
            estrelas = estrelas,
            criadoEm = agora.ToString("s", CultureInfo.InvariantCulture)
        };
        File.WriteAllBytes(Caminho(novo), png);
        marcadores.Add(novo);

        while (ContarVistas(gtin) > VistasPorProduto)
        {
            Marcador antigo = marcadores.Find(m => m.gtin == gtin);
            marcadores.Remove(antigo);
            File.Delete(Caminho(antigo));
            removidos?.Add(antigo);
        }
        Salvar();
        return novo;
    }

    // null se o PNG sumiu da pasta
    public byte[] LerImagem(Marcador marcador)
    {
        string caminho = Caminho(marcador);
        return File.Exists(caminho) ? File.ReadAllBytes(caminho) : null;
    }

    // So o nome do arquivo, mesmo que o indice tenha sido mexido: nada sai da pasta do catalogo
    string Caminho(Marcador marcador)
    {
        return Path.Combine(pasta, Path.GetFileName(marcador.arquivo));
    }

    List<Marcador> Carregar()
    {
        string indice = Path.Combine(pasta, ArquivoIndice);
        if (!File.Exists(indice)) return new List<Marcador>();
        try
        {
            var lidos = JsonConvert.DeserializeObject<List<Marcador>>(File.ReadAllText(indice)) ?? new List<Marcador>();
            // Entrada sem arquivo ou com GTIN estragado nao vira marcador
            lidos.RemoveAll(m => m == null || string.IsNullOrEmpty(m.arquivo) || !DadosProduto.GtinValido(m.gtin));
            return lidos;
        }
        catch (Exception e) when (e is JsonException || e is IOException || e is UnauthorizedAccessException)
        {
            Debug.LogWarning($"[RA] Indice de marcadores ensinados ilegivel ({e.Message}); comecando vazio.");
            return new List<Marcador>();
        }
    }

    // Escreve num arquivo temporario e so depois troca pelo indice: se o app morrer no meio, o indice antigo fica
    void Salvar()
    {
        string indice = Path.Combine(pasta, ArquivoIndice);
        string temporario = indice + ".tmp";
        File.WriteAllText(temporario, JsonConvert.SerializeObject(marcadores, Formatting.Indented));
        if (File.Exists(indice)) File.Replace(temporario, indice, null);
        else File.Move(temporario, indice);
    }
}
