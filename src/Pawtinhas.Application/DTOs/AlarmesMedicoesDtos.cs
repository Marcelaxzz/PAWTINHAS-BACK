using System.ComponentModel.DataAnnotations;
using Pawtinhas.Domain;

namespace Pawtinhas.Application.DTOs;

// ---------- Alarmes ----------

public sealed record SalvarAlarmeDto(
    [Required(ErrorMessage = "Dê um nome ao alarme."), StringLength(60)] string Nome,
    int? AnimalId,
    [StringLength(140)] string? Descricao,
    [Required(ErrorMessage = "Informe o horário.")] TimeOnly? Hora,
    [Required(ErrorMessage = "Escolha a recorrência.")] string Recorrencia,
    bool Ativo = true,
    DateOnly? Inicio = null);

public sealed record AlterarAtivoAlarmeDto(bool Ativo);

public sealed record AlarmeResponseDto(
    int Id, string Nome, int? AnimalId, string? AnimalNome, string? AnimalFotoUrl, string Descricao,
    TimeOnly Hora, string Recorrencia, DateOnly Inicio, bool Ativo)
{
    public static AlarmeResponseDto FromEntity(Alarme a) =>
        new(a.Id, a.Nome, a.AnimalId, a.Animal?.Nome, a.Animal?.FotoUrl, a.Descricao, a.Hora, a.Recorrencia, a.Inicio, a.Ativo);
}

public sealed record DisparoAlarmeResponseDto(int Id, int? AlarmeId, string Nome, DateTime Quando, string Resultado)
{
    public static DisparoAlarmeResponseDto FromEntity(DisparoAlarme d) => new(d.Id, d.AlarmeId, d.Nome, d.Quando, d.Resultado);
}

// ---------- ESP32 ----------

/// <summary>Alarme no formato curto do ESP32. Inicio vai como "aaaa-mm-dd".</summary>
public sealed record AlarmeDispositivoDto(int Id, string Nome, string Hora, string Recorrencia, string Inicio, string Descricao, string? Pet);

/// <summary>Quando: hora local em que tocou (o ESP32 guarda os disparos sem internet e manda depois).</summary>
public sealed record DisparoDispositivoDto(
    [Range(1, int.MaxValue, ErrorMessage = "Informe o alarme.")] int AlarmeId,
    [Required(ErrorMessage = "Informe o resultado."), StringLength(60)] string Resultado,
    DateTime? Quando = null);

