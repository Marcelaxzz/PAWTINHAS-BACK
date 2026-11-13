using Pawtinhas.Application.DTOs;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Services;

/// <summary>
/// Área do tutor logado. Toda consulta é filtrada pelo tutor da conta, então ninguém enxerga dado de outro.
/// Animal de outro tutor responde como "não encontrado" (e não "proibido"), para não revelar que o id existe.
/// </summary>
public class TutorAppService
{
    private readonly IAnimalRepository _animais;
    private readonly IEventoRepository _eventos;

    public TutorAppService(IAnimalRepository animais, IEventoRepository eventos)
    {
        _animais = animais;
        _eventos = eventos;
    }

    public async Task<IReadOnlyList<MeuAnimalDto>> ListarAnimaisAsync(int responsavelId) =>
        (await _animais.ListarPorResponsavelAsync(responsavelId)).Select(MeuAnimalDto.De).ToList();

    public async Task<MeuAnimalDto?> ObterAnimalAsync(int responsavelId, int animalId)
    {
        var animal = await _animais.ObterPorIdAsync(animalId);
        return animal is not null && animal.ResponsavelId == responsavelId ? MeuAnimalDto.De(animal) : null;
    }

    public async Task<IReadOnlyList<MeuCompromissoDto>> ListarCompromissosAsync(int responsavelId, int? animalId) =>
        (await _eventos.ListarDoResponsavelAsync(responsavelId, animalId)).Select(MeuCompromissoDto.De).ToList();
}