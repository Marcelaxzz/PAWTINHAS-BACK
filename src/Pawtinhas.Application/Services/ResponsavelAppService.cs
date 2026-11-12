
using Pawtinhas.Application.DTOs;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Comum;
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
            dto.NomeCompleto,
            dto.Telefone,
            dto.CPF,
            dto.Email,
            dto.CEP,
            dto.Endereco,
            dto.Cidade,
            dto.UF);

        await GarantirCpfLivreAsync(responsavel.CPF, null);
        await _repository.AdicionarAsync(responsavel);
        await _unitOfWork.CommitAsync();

        return ResponsavelResponseDto.FromEntity(responsavel);
    }

    public async Task<IReadOnlyList<ResponsavelResponseDto>> ListarAsync()
    {
        var responsaveis = await _repository.ObterTodosAsync();

        return responsaveis
            .OrderBy(r => r.NomeCompleto)
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

    public async Task<ResponsavelResponseDto?> AtualizarAsync(
        int id,
        CadastrarResponsavelDto dto)
    {
        var responsavel = await _repository.ObterPorIdAsync(id);

        if (responsavel is null)
            return null;

        responsavel.AtualizarDados(
            dto.NomeCompleto,
            dto.Telefone,
            dto.CPF,
            dto.Email,
            dto.CEP,
            dto.Endereco,
            dto.Cidade,
            dto.UF);

        await GarantirCpfLivreAsync(responsavel.CPF, id);
        _repository.Atualizar(responsavel);
        await _unitOfWork.CommitAsync();

        return ResponsavelResponseDto.FromEntity(responsavel);
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        var responsavel = await _repository.ObterPorIdAsync(id);

        if (responsavel is null)
            return false;

        if (await _repository.TemAnimaisAsync(id))
            throw new ConflitoException("tutor_com_animais",
                "Este tutor ainda tem animais cadastrados. Transfira ou exclua os animais antes.");

        _repository.Remover(responsavel);
        await _unitOfWork.CommitAsync();

        return true;
    }

    private async Task GarantirCpfLivreAsync(string? cpf, int? ignorarId)
    {
        if (cpf is not null && await _repository.CpfEmUsoAsync(cpf, ignorarId))
            throw new DominioException("cpf", "Já existe um tutor com este CPF.");
    }
}
