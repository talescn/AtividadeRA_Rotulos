using System.Collections.Generic;

// Dados de um produto: vem do JSON em Resources/Produtos/{gtin}.json, do cache no aparelho
// ou do Open Food Facts. Numero ausente ou ilegivel no rotulo fica null (nunca inventado).
[System.Serializable]
public class DadosProduto
{
    public const string FonteRotulo = "rotulo";
    public const string FonteOpenFoodFacts = "openfoodfacts";

    public string gtin;
    public string nome;
    public string marca;
    // Data em que os valores foram conferidos, no formato AAAA-MM-DD
    public string conferidoEm;
    // FonteRotulo (transcrito da embalagem) ou FonteOpenFoodFacts
    public string fonte;
    public Porcao porcao;
    public int? porcoesPorEmbalagem;
    // A que os valores "por 100" se referem: "100 ml" ou "100 g"
    public string base100;
    // Observacao do rotulo sobre a coluna "por 100" (ex.: no Maggi, o alimento pronto para o consumo)
    public string observacaoBase100;
    public List<Nutriente> nutrientes = new List<Nutriente>();
    public string ingredientes;
    // Frase de alergenicos como esta no rotulo ("ALERGICOS: CONTEM ...")
    public string alergenos;

    [System.Serializable]
    public class Porcao
    {
        // Medida caseira, como no rotulo: "1 copo", "1 unidade"
        public string descricao;
        public float? quantidade;
        // "g" ou "ml"
        public string unidade;
    }

    [System.Serializable]
    public class Nutriente
    {
        // Identificador estavel (energia, carboidratos, sodio...); o nome e so para exibir
        public string chave;
        public string nome;
        // "kcal", "g", "mg" ou "µg"
        public string unidade;
        public float? por100;
        public float? porPorcao;
        // %VD da porcao
        public float? vd;
        // 0 = linha principal; 1 e 2 = "dos quais" (ex.: acucares dentro de carboidratos)
        public int nivel;
    }

    public Nutriente Buscar(string chave)
    {
        foreach (var n in nutrientes)
            if (n.chave == chave) return n;
        return null;
    }

    // So da para desenhar a tabela se houver ao menos o valor energetico
    public bool TemTabela => Buscar("energia") != null;

    // Digito verificador do GTIN-8, 12, 13 ou 14: pesos 3 e 1 alternados, da direita para a esquerda
    public static bool GtinValido(string gtin)
    {
        if (gtin == null || (gtin.Length != 8 && gtin.Length != 12 && gtin.Length != 13 && gtin.Length != 14)) return false;
        int soma = 0;
        for (int i = gtin.Length - 2, peso = 3; i >= 0; i--, peso = 4 - peso)
        {
            if (gtin[i] < '0' || gtin[i] > '9') return false;
            soma += (gtin[i] - '0') * peso;
        }
        char ultimo = gtin[gtin.Length - 1];
        return ultimo >= '0' && ultimo <= '9' && (10 - soma % 10) % 10 == ultimo - '0';
    }
}
