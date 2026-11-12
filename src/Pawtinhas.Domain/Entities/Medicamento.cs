using Pawtinhas.Domain.Calculos;
using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>
/// Medicamento do catálogo. Concentração e faixas de dose (mg/kg) vêm da bula ou da literatura,
/// e a <see cref="Fonte"/> fica registrada para quem prescreve conferir de onde veio cada número.
/// </summary>
public class Medicamento
{
    public static readonly string[] Unidades = ["comprimido", "cápsula", "mL", "gota"];

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string PrincipioAtivo { get; private set; } = string.Empty;
    public string Forma { get; private set; } = string.Empty;

    /// <summary>Unidade de administração: comprimido, cápsula, mL ou gota.</summary>
    public string Unidade { get; private set; } = string.Empty;

    /// <summary>Miligramas por unidade (mg/comprimido, mg/mL, mg/gota...).</summary>
    public decimal Concentracao { get; private set; }

    /// <summary>Comprimido sulcado que pode ser partido em até 1/4.</summary>
    public bool Fracionavel { get; private set; }

    public string Via { get; private set; } = string.Empty;
    public decimal? DoseMinCao { get; private set; }
    public decimal? DoseMaxCao { get; private set; }
    public decimal? DoseMinGato { get; private set; }
    public decimal? DoseMaxGato { get; private set; }

    /// <summary>Teto absoluto por dose, em mg, independentemente do peso.</summary>
    public decimal? DoseMaximaMg { get; private set; }

    public int? IntervaloPadraoHoras { get; private set; }
    public string Observacoes { get; private set; } = string.Empty;

    /// <summary>Bula, consenso ou manual de onde vieram concentração e faixas.</summary>
    public string Fonte { get; private set; } = string.Empty;

    private Medicamento() { }

    public Medicamento(
        string nome, string principioAtivo, string forma, string unidade, decimal concentracao, bool fracionavel,
        string? via, decimal? doseMinCao, decimal? doseMaxCao, decimal? doseMinGato, decimal? doseMaxGato,
        decimal? doseMaximaMg, int? intervaloPadraoHoras, string? observacoes, string? fonte) =>
        Atualizar(nome, principioAtivo, forma, unidade, concentracao, fracionavel, via, doseMinCao, doseMaxCao,
            doseMinGato, doseMaxGato, doseMaximaMg, intervaloPadraoHoras, observacoes, fonte);

    public void Atualizar(
        string nome, string principioAtivo, string forma, string unidade, decimal concentracao, bool fracionavel,
        string? via, decimal? doseMinCao, decimal? doseMaxCao, decimal? doseMinGato, decimal? doseMaxGato,
        decimal? doseMaximaMg, int? intervaloPadraoHoras, string? observacoes, string? fonte)
    {
        Nome = Validar.Texto(nome, "nome", 80, "Informe o nome comercial ou a apresentação.");
        PrincipioAtivo = Validar.Texto(principioAtivo, "principioAtivo", 80, "Informe o princípio ativo.");
        Forma = Validar.Texto(forma, "forma", 40, "Informe a forma farmacêutica.");
        Unidade = Validar.Opcao(unidade, Unidades, "unidade", "Escolha a unidade de administração.");
        Concentracao = Validar.Faixa(concentracao, 0.001m, 100000m, "concentracao", "Informe a concentração (mg por unidade).");
        Fracionavel = Unidade == "comprimido" && fracionavel;
        Via = Validar.TextoLivre(via, "via", 20);
        (DoseMinCao, DoseMaxCao) = Faixa(doseMinCao, doseMaxCao, "doseMinCao", "doseMaxCao");
        (DoseMinGato, DoseMaxGato) = Faixa(doseMinGato, doseMaxGato, "doseMinGato", "doseMaxGato");
        DoseMaximaMg = doseMaximaMg is { } teto
            ? Validar.Faixa(teto, 0.001m, 100000m, "doseMaximaMg", "Informe o teto em mg.")
            : null;
        if (intervaloPadraoHoras is { } h && !CalculosClinicos.Intervalos.Contains(h))
            throw new DominioException("intervaloPadraoHoras", "Escolha um intervalo válido.");
        IntervaloPadraoHoras = intervaloPadraoHoras;
        Observacoes = Validar.TextoLivre(observacoes, "observacoes", 500);
        Fonte = Validar.TextoLivre(fonte, "fonte", 300);
    }

    private static (decimal?, decimal?) Faixa(decimal? min, decimal? max, string campoMin, string campoMax)
    {
        if (min is { } a) Validar.Faixa(a, 0.0001m, 1000m, campoMin, "Informe a dose em mg/kg.");
        if (max is { } b) Validar.Faixa(b, 0.0001m, 1000m, campoMax, "Informe a dose em mg/kg.");
        if (min is { } x && max is { } y && x > y)
            throw new DominioException(campoMax, "A dose máxima deve ser maior ou igual à mínima.");
        return (min, max);
    }
}
