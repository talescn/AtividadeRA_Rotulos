using System.Collections.Generic;

// Lupa da rotulagem nutricional frontal ("ALTO EM ...") calculada a partir da tabela.
// Limites do Anexo XV da IN 75/2020: maior ou igual a estes valores por 100 g (solido) ou 100 ml (liquido).
// RDC 429/2020, art. 19: o limite vale para o alimento como exposto a venda ou, se ele precisa de preparo,
// pronto para o consumo, que e a coluna "por 100" do rotulo nesse caso (como no Maggi).
// Os alimentos do Anexo XVI da IN 75 (leites, queijos, oleos...) nao podem ter lupa: o JSON diz isso em isencaoLupa.
public static class RotulagemFrontal
{
    public enum Estado { Abaixo, Alto, SemDado }

    public class Item
    {
        // acucaresAdicionados, gordurasSaturadas ou sodio
        public string chave;
        // Como na lupa do rotulo: "AÇÚCAR ADICIONADO"
        public string rotulo;
        public Estado estado;
        // Valor comparado com o limite: por 100 g ou 100 ml, na unidade do limite
        public float? valor100;
    }

    public class Resultado
    {
        // Por que a lupa nao se aplica (Anexo XVI); null quando ela se aplica
        public string isencao;
        public List<Item> itens = new List<Item>();

        public List<string> Altos() => Rotulos(Estado.Alto);
        public List<string> FaltaDado() => Rotulos(Estado.SemDado);

        List<string> Rotulos(Estado estado)
        {
            var lista = new List<string>();
            foreach (var item in itens)
                if (item.estado == estado) lista.Add(item.rotulo);
            return lista;
        }
    }

    // Anexo XV da IN 75/2020, na unidade da tabela: g para acucar e gordura, mg para sodio
    static readonly (string chave, string rotulo, string unidade, float solido, float liquido)[] Limites =
    {
        ("acucaresAdicionados", "AÇÚCAR ADICIONADO", "g", 15f, 7.5f),
        ("gordurasSaturadas", "GORDURA SATURADA", "g", 6f, 3f),
        ("sodio", "SÓDIO", "mg", 600f, 300f)
    };

    public static Resultado Avaliar(DadosProduto dados)
    {
        var resultado = new Resultado();
        if (!string.IsNullOrEmpty(dados.isencaoLupa))
        {
            resultado.isencao = dados.isencaoLupa;
            return resultado;
        }

        bool liquido = CalculosPorcao.UnidadeBase(dados) == "ml";
        foreach (var limite in Limites)
        {
            var n = dados.Buscar(limite.chave);
            float? valor = Converter(CalculosPorcao.Por100(n, dados), n?.unidade, limite.unidade);
            // Sem valor fica SemDado, nunca Abaixo: ausencia de dado nao e teor baixo
            Estado estado = !valor.HasValue ? Estado.SemDado
                : valor.Value >= (liquido ? limite.liquido : limite.solido) ? Estado.Alto : Estado.Abaixo;
            resultado.itens.Add(new Item { chave = limite.chave, rotulo = limite.rotulo, estado = estado, valor100 = valor });
        }
        return resultado;
    }

    // "A lupa não se aplica a leites (IN 75/2020, Anexo XVI, item 7)."; null quando a lupa se aplica
    public static string TextoIsencao(Resultado resultado)
    {
        return resultado.isencao == null ? null : "A lupa não se aplica a " + resultado.isencao + ".";
    }

    // "Sem dado de açúcar adicionado neste rótulo."; null quando nada falta
    public static string TextoFaltaDado(Resultado resultado)
    {
        var faltam = resultado.FaltaDado();
        return faltam.Count == 0 ? null : "Sem dado de " + Juntar(Minusculas(faltam)) + " neste rótulo.";
    }

    // "a", "a e b", "a, b e c"
    public static string Juntar(IList<string> partes)
    {
        if (partes.Count == 0) return "";
        if (partes.Count == 1) return partes[0];
        var inicio = new List<string>(partes);
        inicio.RemoveAt(inicio.Count - 1);
        return string.Join(", ", inicio) + " e " + partes[partes.Count - 1];
    }

    // "AÇÚCAR ADICIONADO" -> "açúcar adicionado", para usar no meio de uma frase
    public static List<string> Minusculas(IEnumerable<string> textos)
    {
        var lista = new List<string>();
        foreach (var texto in textos) lista.Add(texto.ToLowerInvariant());
        return lista;
    }

    // Converte entre g, mg e µg; sem unidade, supoe a do limite
    static float? Converter(float? valor, string de, string para)
    {
        if (!valor.HasValue) return null;
        if (string.IsNullOrEmpty(de) || de == para) return valor;
        double fatorDe = Gramas(de), fatorPara = Gramas(para);
        if (fatorDe == 0 || fatorPara == 0) return null;
        return (float)(valor.Value * fatorDe / fatorPara);
    }

    // Quantos gramas vale uma unidade
    static double Gramas(string unidade)
    {
        switch (unidade)
        {
            case "g": return 1;
            case "mg": return 1e-3;
            case "µg": return 1e-6;
            default: return 0;
        }
    }
}
