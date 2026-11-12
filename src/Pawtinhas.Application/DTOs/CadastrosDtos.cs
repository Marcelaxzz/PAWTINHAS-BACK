using System.ComponentModel.DataAnnotations;
using Pawtinhas.Domain;

namespace Pawtinhas.Application.DTOs;

// ---------- Equipe ----------

public sealed record SalvarProfissionalDto(
    [Required(ErrorMessage = "Informe o nome."), StringLength(80)] string Nome,
    [Required(ErrorMessage = "Escolha a função.")] string Funcao,
    [StringLength(150)] string? Email);

public sealed record ProfissionalResponseDto(int Id, string Nome, string Funcao, string Papel, string Iniciais, string? Email)
{
    public static ProfissionalResponseDto FromEntity(Profissional p) => new(p.Id, p.Nome, p.Funcao, p.Papel, p.Iniciais, p.Email);
}

// ---------- Catálogo de medicamentos ----------

public sealed record SalvarMedicamentoDto(
    [Required(ErrorMessage = "Informe o nome."), StringLength(80)] string Nome,
    [Required(ErrorMessage = "Informe o princípio ativo."), StringLength(80)] string PrincipioAtivo,
    [Required(ErrorMessage = "Informe a forma farmacêutica."), StringLength(40)] string Forma,
    [Required(ErrorMessage = "Escolha a unidade.")] string Unidade,
    [Required(ErrorMessage = "Informe a concentração.")] decimal? Concentracao,
    bool Fracionavel,
    [StringLength(20)] string? Via,
    decimal? DoseMinCao,
    decimal? DoseMaxCao,
    decimal? DoseMinGato,
    decimal? DoseMaxGato,
    decimal? DoseMaximaMg,
    int? IntervaloPadraoHoras,
    [StringLength(500)] string? Observacoes,
    [StringLength(300)] string? Fonte);

public sealed record MedicamentoResponseDto(
    int Id, string Nome, string PrincipioAtivo, string Forma, string Unidade, decimal Concentracao, bool Fracionavel,
    string Via, decimal? DoseMinCao, decimal? DoseMaxCao, decimal? DoseMinGato, decimal? DoseMaxGato,
    decimal? DoseMaximaMg, int? IntervaloPadraoHoras, string Observacoes, string Fonte)
{
    public static MedicamentoResponseDto FromEntity(Medicamento m) => new(
        m.Id, m.Nome, m.PrincipioAtivo, m.Forma, m.Unidade, m.Concentracao, m.Fracionavel, m.Via,
        m.DoseMinCao, m.DoseMaxCao, m.DoseMinGato, m.DoseMaxGato, m.DoseMaximaMg, m.IntervaloPadraoHoras,
        m.Observacoes, m.Fonte);
}

// ---------- Catálogo de rações ----------

public sealed record SalvarRacaoDto(
    [Required(ErrorMessage = "Informe o nome."), StringLength(80)] string Nome,
    [Required(ErrorMessage = "Escolha a espécie.")] string Especie,
    [Required(ErrorMessage = "Escolha a fase.")] string Fase,
    [Required(ErrorMessage = "Escolha o tipo.")] string Tipo,
    [Required(ErrorMessage = "Informe a energia (kcal/kg).")] decimal? KcalPorKg,
    [StringLength(300)] string? Observacoes,
    [StringLength(500)] string? FotoUrl = null);

public sealed record RacaoResponseDto(
    int Id, string Nome, string Especie, string Fase, string Tipo, decimal KcalPorKg, string Observacoes, string? FotoUrl)
{
    public static RacaoResponseDto FromEntity(Racao r) =>
        new(r.Id, r.Nome, r.Especie, r.Fase, r.Tipo, r.KcalPorKg, r.Observacoes, r.FotoUrl);
}

// ---------- Catálogo de materiais e insumos ----------

public sealed record SalvarMaterialDto(
    [Required(ErrorMessage = "Informe o nome."), StringLength(80)] string Nome,
    [Required(ErrorMessage = "Escolha o tipo.")] string Tipo,
    [Required(ErrorMessage = "Escolha a unidade.")] string Unidade,
    [StringLength(80)] string? Especificacao,
    [StringLength(300)] string? Observacoes);

public sealed record MaterialResponseDto(int Id, string Nome, string Tipo, string Unidade, string Especificacao, string Observacoes)
{
    public static MaterialResponseDto FromEntity(Material m) => new(m.Id, m.Nome, m.Tipo, m.Unidade, m.Especificacao, m.Observacoes);
}
