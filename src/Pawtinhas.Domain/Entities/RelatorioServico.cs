using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>
/// Relatório preenchido pela veterinária para um atendimento da agenda (consulta, cirurgia, banho,
/// visita, emergência ou vacina). Um por atendimento; vira o PDF entregue ao tutor.
/// </summary>
public class RelatorioServico
{
    public int Id { get; private set; }
    public int EventoId { get; private set; }

    /// <summary>Motivo da visita ou queixa principal contada pelo tutor.</summary>
    public string Queixa { get; private set; } = string.Empty;

    /// <summary>Exame físico e achados (peso, temperatura, mucosas, comportamento...).</summary>
    public string Avaliacao { get; private set; } = string.Empty;

    public string Procedimentos { get; private set; } = string.Empty;
    public string Diagnostico { get; private set; } = string.Empty;

    /// <summary>Medicação, cuidados em casa e orientações ao tutor.</summary>
    public string Orientacoes { get; private set; } = string.Empty;

    public decimal? PesoKg { get; private set; }
    public decimal? TemperaturaC { get; private set; }
    public DateOnly? Retorno { get; private set; }

    /// <summary>Quem assina o relatório (nome e registro no CRMV, por exemplo).</summary>
    public string Responsavel { get; private set; } = string.Empty;

    public DateTime CriadoEm { get; private set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; private set; } = DateTime.UtcNow;

    public Evento? Evento { get; private set; }

    private RelatorioServico() { }

    public RelatorioServico(int eventoId) => EventoId = eventoId;

    public void Preencher(
        string? queixa, string? avaliacao, string? procedimentos, string? diagnostico, string? orientacoes,
        decimal? pesoKg, decimal? temperaturaC, DateOnly? retorno, string? responsavel, DateOnly dataAtendimento)
    {
        Queixa = Validar.TextoLivre(queixa, "queixa", 1000);
        Avaliacao = Validar.TextoLivre(avaliacao, "avaliacao", 2000);
        Procedimentos = Validar.TextoLivre(procedimentos, "procedimentos", 2000);
        Diagnostico = Validar.TextoLivre(diagnostico, "diagnostico", 1000);
        Orientacoes = Validar.TextoLivre(orientacoes, "orientacoes", 2000);
        PesoKg = pesoKg is { } p ? Validar.Faixa(p, 0.01m, 200m, "pesoKg", "Informe o peso entre 0,01 e 200 kg.") : null;
        TemperaturaC = temperaturaC is { } t
            ? Validar.Faixa(t, 30m, 45m, "temperaturaC", "Informe a temperatura entre 30 e 45 °C.")
            : null;
        if (retorno is { } r && r < dataAtendimento)
            throw new DominioException("retorno", "O retorno precisa ser depois do atendimento.");
        Retorno = retorno;
        Responsavel = Validar.TextoLivre(responsavel, "responsavel", 120);

        if (new[] { Queixa, Avaliacao, Procedimentos, Diagnostico, Orientacoes }.All(string.IsNullOrEmpty))
            throw new DominioException("procedimentos", "Preencha ao menos um campo do relatório.");
        AtualizadoEm = DateTime.UtcNow;
    }
}
