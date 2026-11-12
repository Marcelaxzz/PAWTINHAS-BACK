using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>
/// Material e insumo da clínica (curativos, fluidoterapia, instrumental cirúrgico, descartáveis).
/// Não entra na calculadora de dose: existe para o controle de estoque.
/// </summary>
public class Material
{
    public static readonly string[] Tipos =
        ["Curativo", "Fluidoterapia", "Cirúrgico", "Descartável", "Antissepsia"];

    /// <summary>Unidade em que o item é contado no estoque.</summary>
    public static readonly string[] Unidades = ["unidade", "pacote", "caixa", "frasco", "bolsa", "rolo", "par", "kit"];

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Tipo { get; private set; } = string.Empty;
    public string Unidade { get; private set; } = string.Empty;

    /// <summary>Tamanho, volume ou numeração ("7,5 x 7,5 cm", "500 mL", "nº 7,5").</summary>
    public string Especificacao { get; private set; } = string.Empty;

    public string Observacoes { get; private set; } = string.Empty;

    private Material() { }

    public Material(string nome, string tipo, string unidade, string? especificacao, string? observacoes) =>
        Atualizar(nome, tipo, unidade, especificacao, observacoes);

    public void Atualizar(string nome, string tipo, string unidade, string? especificacao, string? observacoes)
    {
        Nome = Validar.Texto(nome, "nome", 80, "Informe o nome do material.");
        Tipo = Validar.Opcao(tipo, Tipos, "tipo", "Escolha o tipo.");
        Unidade = Validar.Opcao(unidade, Unidades, "unidade", "Escolha a unidade de contagem.");
        Especificacao = Validar.TextoLivre(especificacao, "especificacao", 80);
        Observacoes = Validar.TextoLivre(observacoes, "observacoes", 300);
    }
}
