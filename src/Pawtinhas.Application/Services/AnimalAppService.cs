
using Pawtinhas.Application.Abstracoes;
using Pawtinhas.Application.DTOs;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Comum;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Services;

/// <summary>Pacientes da clínica (animais com responsável).</summary>
public class AnimalAppService
{
    private readonly IAnimalRepository _repository;
    private readonly IResponsavelRepository _responsavelRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IArmazenamentoFotos _fotos;

    public AnimalAppService(
        IAnimalRepository repository,
        IResponsavelRepository responsavelRepository,
        IUnitOfWork unitOfWork,
        IArmazenamentoFotos fotos)
    {
        _repository = repository;
        _responsavelRepository = responsavelRepository;
        _unitOfWork = unitOfWork;
        _fotos = fotos;
    }

    public async Task<AnimalResponseDto> CadastrarAnimalAsync(
        CadastrarAnimalDto dto)
    {
        await GarantirResponsavelAsync(dto.ResponsavelId);

        var animal = new Animal(
            dto.ResponsavelId,
            dto.Nome,
            dto.Especie ?? string.Empty,
            dto.Sexo,
            dto.Raca,
            dto.DataNascimento,
            dto.Porte,
            dto.PesoKg,
            dto.Observacoes,
            _fotos.ValidarUrl(dto.FotoUrl, "fotoUrl"),
            dto.Localizacao,
            dto.Personalidade);

        await _repository.AdicionarAsync(animal);
        await _unitOfWork.CommitAsync();

        return AnimalResponseDto.FromEntity(await _repository.ObterPorIdAsync(animal.Id) ?? animal);
    }

    public async Task<AnimalResponseDto?> ObterAnimalPorIdAsync(int id)
    {
        var animal = await _repository.ObterPorIdAsync(id);

        return animal is null || !animal.EhPaciente
            ? null
            : AnimalResponseDto.FromEntity(animal);
    }

    public async Task<IReadOnlyList<AnimalResponseDto>> ListarAnimaisAsync()
    {
        var animais = await _repository.ListarPacientesAsync();

        return animais
            .Select(AnimalResponseDto.FromEntity)
            .ToList();
    }

    public async Task<AnimalResponseDto?> AtualizarAnimalAsync(
        int id,
        CadastrarAnimalDto dto)
    {
        var animal = await _repository.ObterPorIdAsync(id);

        if (animal is null || !animal.EhPaciente)
            return null;

        await GarantirResponsavelAsync(dto.ResponsavelId);

        var fotoAnterior = animal.FotoUrl;
        animal.AtualizarDados(
            dto.ResponsavelId,
            dto.Nome,
            dto.Especie ?? string.Empty,
            dto.Sexo,
            dto.Raca,
            dto.DataNascimento,
            dto.Porte,
            dto.PesoKg,
            dto.Observacoes,
            _fotos.ValidarUrl(dto.FotoUrl, "fotoUrl"),
            dto.Localizacao,
            dto.Personalidade);

        _repository.Atualizar(animal);
        await _unitOfWork.CommitAsync();

        if (fotoAnterior != animal.FotoUrl) _fotos.RemoverSeLocal(fotoAnterior);
        return AnimalResponseDto.FromEntity(await _repository.ObterPorIdAsync(id) ?? animal);
    }

    /// <summary>Remove o paciente com a agenda, as medições e o histórico clínico dele.</summary>
    public async Task<bool> ExcluirAnimalAsync(int id)
    {
        var animal = await _repository.ObterPorIdAsync(id);

        if (animal is null || !animal.EhPaciente)
            return false;

        _repository.Remover(animal);
        await _unitOfWork.CommitAsync();
        _fotos.RemoverSeLocal(animal.FotoUrl);

        return true;
    }

    private async Task GarantirResponsavelAsync(int responsavelId)
    {
        if (await _responsavelRepository.ObterPorIdAsync(responsavelId) is null)
            throw new DominioException("responsavelId", "Tutor não encontrado. Selecione outro.");
    }
}
