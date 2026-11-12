using Pawtinhas.Domain.Calculos;
using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>
/// Prescrição calculada por mg/kg. Os valores derivados (dose, quantidade, total) saem do
/// <see cref="CalculosClinicos"/> e ficam gravados como foram prescritos.
/// </summary>
public class Prescricao
{
    public static readonly string[] Situacoes = ["Ativa", "Suspensa", "Concluída"];

    public int Id { get; private set; }
    public int AnimalId { get; private set; }
    public int? MedicamentoId { get; private set; }

    /// <summary>Dados do medicamento no momento da prescrição (o catálogo pode mudar depois).</summary>
    public string Medicamento { get; private set; } = string.Empty;
    public string Unidade { get; private set; } = string.Empty;
    public decimal Concentracao { get; private set; }
    public string Via { get; private set; } = string.Empty;

    public decimal PesoKg { get; private set; }
    public decimal DoseMgKg { get; private set; }
    public decimal DoseMg { get; private set; }
    public decimal QuantidadePorDose { get; private set; }
    public decimal DoseRealMg { get; private set; }
    public int IntervaloHoras { get; private set; }
    public int DuracaoDias { get; private set; }
    public int TotalDoses { get; private set; }
    public decimal QuantidadeTotal { get; private set; }
    public DateOnly Inicio { get; private set; }
    public TimeOnly HoraPrimeiraDose { get; private set; }
    public string Status { get; private set; } = "Ativa";

    /// <summary>Alertas que a veterinária confirmou ao salvar (fora da faixa, arredondamento...).</summary>
    public string AlertasConfirmados { get; private set; } = string.Empty;

    public string Observacoes { get; private set; } = string.Empty;
    public int? AlarmeId { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private Prescricao() { }

    public Prescricao(
        int animalId, int? medicamentoId, string medicamento, string via, EntradaDose entrada, ResultadoDose resultado,
        DateOnly inicio, TimeOnly horaPrimeiraDose, string? observacoes, DateTime criadoEm)
    {
        AnimalId = animalId;
        MedicamentoId = medicamentoId;
        Medicamento = Validar.Texto(medicamento, "medicamento", 80, "Informe o medicamento.");
        Unidade = entrada.Unidade;
        Concentracao = entrada.Concentracao;
        Via = Validar.TextoLivre(via, "via", 20);
        PesoKg = entrada.PesoKg;
        DoseMgKg = entrada.DoseMgKg;
        DoseMg = resultado.DoseMg;
        QuantidadePorDose = resultado.QuantidadePorDose;
        DoseRealMg = resultado.DoseRealMg;
        IntervaloHoras = entrada.IntervaloHoras;
        DuracaoDias = entrada.DuracaoDias;
        TotalDoses = resultado.TotalDoses;
        QuantidadeTotal = resultado.QuantidadeTotal;
        Inicio = inicio;
        HoraPrimeiraDose = horaPrimeiraDose;
        var alertas = string.Join(" | ", resultado.Alertas);
        AlertasConfirmados = alertas.Length > 1000 ? alertas[..1000] : alertas;
        Observacoes = Validar.TextoLivre(observacoes, "observacoes", 500);
        CriadoEm = criadoEm;
    }

    public void VincularAlarme(Alarme alarme) => AlarmeId = alarme.Id;

    public void AlterarStatus(string status) =>
        Status = Validar.Opcao(status, Situacoes, "status", "Escolha um status válido.");

    public void AlterarObservacoes(string? observacoes) =>
        Observacoes = Validar.TextoLivre(observacoes, "observacoes", 500);
}
