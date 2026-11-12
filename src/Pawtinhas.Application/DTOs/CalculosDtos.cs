using System.ComponentModel.DataAnnotations;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Calculos;

namespace Pawtinhas.Application.DTOs;

public sealed class DoseRequisicao
{
    public int? AnimalId { get; set; }

    /// <summary>Só para a simulação sem paciente; com paciente vale a espécie do cadastro.</summary>
    [StringLength(40)] public string? Especie { get; set; }

    public int? MedicamentoId { get; set; }
    [StringLength(80)] public string? Medicamento { get; set; }
    [StringLength(20)] public string? Unidade { get; set; }
    public decimal? Concentracao { get; set; }
    public bool Fracionavel { get; set; }
    [StringLength(20)] public string? Via { get; set; }
    public decimal? PesoKg { get; set; }
    public decimal? DoseMgKg { get; set; }
    public int? IntervaloHoras { get; set; }
    public int? DuracaoDias { get; set; }
    public DateOnly? Inicio { get; set; }
    public TimeOnly? HoraPrimeiraDose { get; set; }
    [StringLength(500)] public string? Observacoes { get; set; }
    public bool ConfirmarAlertas { get; set; }
    public bool CriarAlarme { get; set; }
    public bool AtualizarPesoPaciente { get; set; }
}

public sealed class AlimentacaoRequisicao
{
    public int? AnimalId { get; set; }
    [StringLength(40)] public string? Especie { get; set; }
    public int? RacaoId { get; set; }
    [StringLength(80)] public string? Racao { get; set; }
    public decimal? KcalPorKg { get; set; }
    [StringLength(40)] public string? Condicao { get; set; }
    public decimal? FatorPersonalizado { get; set; }
    public decimal? PesoAtualKg { get; set; }
    public decimal? PesoMetaKg { get; set; }
    public int? Refeicoes { get; set; }
    [StringLength(500)] public string? Observacoes { get; set; }
    public bool AtualizarPesoPaciente { get; set; }
}

public sealed class PrescricaoAlteracao
{
    [StringLength(20)] public string? Status { get; set; }
    [StringLength(500)] public string? Observacoes { get; set; }
}

public sealed record FatoresResposta(IReadOnlyList<FatorEnergetico> Fatores, int[] Intervalos, IReadOnlyList<string> Fontes);

public sealed record PrescricaoResponseDto(
    int Id, int AnimalId, int? MedicamentoId, string Medicamento, string Unidade, decimal Concentracao, string Via,
    decimal PesoKg, decimal DoseMgKg, decimal DoseMg, decimal QuantidadePorDose, decimal DoseRealMg, int IntervaloHoras,
    int DuracaoDias, int TotalDoses, decimal QuantidadeTotal, DateOnly Inicio, TimeOnly HoraPrimeiraDose, string Status,
    string AlertasConfirmados, string Observacoes, int? AlarmeId, DateTime CriadoEm)
{
    public static PrescricaoResponseDto FromEntity(Prescricao p) => new(
        p.Id, p.AnimalId, p.MedicamentoId, p.Medicamento, p.Unidade, p.Concentracao, p.Via, p.PesoKg, p.DoseMgKg,
        p.DoseMg, p.QuantidadePorDose, p.DoseRealMg, p.IntervaloHoras, p.DuracaoDias, p.TotalDoses, p.QuantidadeTotal,
        p.Inicio, p.HoraPrimeiraDose, p.Status, p.AlertasConfirmados, p.Observacoes, p.AlarmeId, p.CriadoEm);
}

public sealed record PlanoAlimentarResponseDto(
    int Id, int AnimalId, int? RacaoId, string Racao, decimal KcalPorKg, string Especie, decimal PesoAtualKg,
    decimal PesoCalculoKg, string Condicao, decimal Fator, decimal Rer, decimal Mer, int Refeicoes, decimal GramasDia,
    decimal GramasPorRefeicao, decimal KcalPetiscosMax, decimal AguaMinMlDia, decimal AguaMaxMlDia,
    string Observacoes, DateTime CriadoEm)
{
    public static PlanoAlimentarResponseDto FromEntity(PlanoAlimentar p) => new(
        p.Id, p.AnimalId, p.RacaoId, p.Racao, p.KcalPorKg, p.Especie, p.PesoAtualKg, p.PesoCalculoKg, p.Condicao,
        p.Fator, p.Rer, p.Mer, p.Refeicoes, p.GramasDia, p.GramasPorRefeicao, p.KcalPetiscosMax, p.AguaMinMlDia,
        p.AguaMaxMlDia, p.Observacoes, p.CriadoEm);
}
