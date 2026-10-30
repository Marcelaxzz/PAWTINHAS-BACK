
using Pawtinhas.Application.DTOs;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Services;

public class AnimalAppService
{
    private readonly IAnimalRepository _repository;
    private readonly IResponsavelRepository _responsavelRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AnimalAppService(
        IAnimalRepository repository,
        IResponsavelRepository responsavelRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _responsavelRepository = responsavelRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AnimalResponseDto> CadastrarAnimalAsync(
        CadastrarAnimalDto dto)
    {
        var responsavel = await _responsavelRepository
            .ObterPorIdAsync(dto.ResponsavelId);

        if (responsavel is null)
            throw new KeyNotFoundException("Responsável não encontrado.");

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
            dto.FotoUrl,
            dto.Localizacao,
            dto.Personalidade);

        await _repository.AdicionarAsync(animal);
        await _unitOfWork.CommitAsync();

        return AnimalResponseDto.FromEntity(animal);
    }

    public async Task<AnimalResponseDto?> ObterAnimalPorIdAsync(int id)
    {
        var animal = await _repository.ObterPorIdAsync(id);

        return animal is null
            ? null
            : AnimalResponseDto.FromEntity(animal);
    }

    public async Task<IReadOnlyList<AnimalResponseDto>> ListarAnimaisAsync()
    {
        var animais = await _repository.ObterTodosAsync();

        return animais
            .Select(AnimalResponseDto.FromEntity)
            .ToList();
    }

    public async Task<bool> AtualizarAnimalAsync(
        int id,
        CadastrarAnimalDto dto)
    {
        var animal = await _repository.ObterPorIdAsync(id);

        if (animal is null)
            return false;

        var responsavel = await _responsavelRepository
            .ObterPorIdAsync(dto.ResponsavelId);

        if (responsavel is null)
            throw new KeyNotFoundException("Responsável não encontrado.");

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
            dto.FotoUrl,
            dto.Localizacao,
            dto.Personalidade);

        _repository.Atualizar(animal);
        await _unitOfWork.CommitAsync();

        return true;
    }

    public async Task<bool> ExcluirAnimalAsync(int id)
    {
        var animal = await _repository.ObterPorIdAsync(id);

        if (animal is null)
            return false;

        _repository.Remover(animal);
        await _unitOfWork.CommitAsync();

        return true;
    }
}
