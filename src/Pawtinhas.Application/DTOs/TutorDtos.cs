using Pawtinhas.Domain;
using Pawtinhas.Domain.Enums;

namespace Pawtinhas.Application.DTOs;

/// <summary>O que o tutor enxerga do próprio animal. Sem as observações internas da clínica.</summary>
public sealed record MeuAnimalDto(
    int Id, string Nome, string? Especie, string? Raca, string Sexo, string? Idade,
    PorteAnimal? Porte, decimal? PesoKg, string? FotoUrl, SituacaoAnimal Status)
{
    public static MeuAnimalDto De(Animal a) => new(
        a.Id, a.Nome, a.Especie, a.Raca, a.Sexo, a.IdadeTexto(DateTime.Today),
        a.Porte, a.PesoKg, a.FotoUrl, a.Status);
}

/// <summary>Compromisso visto pelo tutor: quando, de quem, com quem e em que situação. Sem anotações internas.</summary>
public sealed record MeuCompromissoDto(
    int Id, string TipoEvento, int? AnimalId, string? AnimalNome,
    DateTime DataHora, DateTime? DataHoraFim, string Status, string? ProfissionalNome)
{
    public static MeuCompromissoDto De(Evento e) => new(
        e.Id, e.GetType().Name, e.AnimalId, e.Animal?.Nome,
        e.DataHora, e is PetSitter p ? (DateTime?)p.DataHoraFim : null, e.Status, e.Profissional?.Nome);
}