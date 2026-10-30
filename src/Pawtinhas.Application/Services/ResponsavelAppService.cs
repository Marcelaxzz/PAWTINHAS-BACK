
using Pawtinhas.Application.DTOs;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Services;

public class ResponsavelAppService
{
    private readonly IResponsavelRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ResponsavelAppService(
        IResponsavelRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ResponsavelResponseDto> CadastrarAsync(
        CadastrarResponsavelDto dto)
    {
        var responsavel = new Responsavel(
            dto.NomeCompleto.Trim(),
            dto.Telefone.Trim(),
            NormalizarOpcional(dto.CPF),
            NormalizarOpcional(dto.Email),
            NormalizarOpcional(dto.CEP),
            NormalizarOpcional(dto.Endereco),
            NormalizarOpcional(dto.Cidade),
            NormalizarOpcional(dto.UF)?.ToUpperInvariant());

        await _repository.AdicionarAsync(responsavel);
        await _unitOfWork.CommitAsync();

        return ResponsavelResponseDto.FromEntity(responsavel);
    }

    public async Task<IReadOnlyList<ResponsavelResponseDto>> ListarAsync()
    {
        var responsaveis = await _repository.ObterTodosAsync();

        return responsaveis
            .Select(ResponsavelResponseDto.FromEntity)
            .ToList();
    }

    public async Task<ResponsavelResponseDto?> ObterPorIdAsync(int id)
    {
        var responsavel = await _repository.ObterPorIdAsync(id);

        return responsavel is null
            ? null
            : ResponsavelResponseDto.FromEntity(responsavel);
    }

    public async Task<bool> AtualizarAsync(
        int id,
        CadastrarResponsavelDto dto)
    {
        var responsavel = await _repository.ObterPorIdAsync(id);

        if (responsavel is null)
            return false;

        responsavel.AtualizarDados(
            dto.NomeCompleto.Trim(),
            dto.Telefone.Trim(),
            NormalizarOpcional(dto.CPF),
            NormalizarOpcional(dto.Email),
            NormalizarOpcional(dto.CEP),
            NormalizarOpcional(dto.Endereco),
            NormalizarOpcional(dto.Cidade),
            NormalizarOpcional(dto.UF)?.ToUpperInvariant());

        _repository.Atualizar(responsavel);
        await _unitOfWork.CommitAsync();

        return true;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        var responsavel = await _repository.ObterPorIdAsync(id);

        if (responsavel is null)
            return false;

        _repository.Remover(responsavel);
        await _unitOfWork.CommitAsync();

        return true;
    }

    private static string? NormalizarOpcional(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim();
    }
}
