using Pawtinhas.Application.DTOs;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Services;

/// <summary>Equipe da clínica (aparece nos agendamentos conforme a função).</summary>
public class ProfissionalAppService(IRepository<Profissional> repository, IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<ProfissionalResponseDto>> ListarAsync() =>
        (await repository.ObterTodosAsync()).OrderBy(p => p.Nome).Select(ProfissionalResponseDto.FromEntity).ToList();

    public async Task<ProfissionalResponseDto> CadastrarAsync(SalvarProfissionalDto dto)
    {
        var profissional = new Profissional(dto.Nome, dto.Funcao, dto.Email);
        await repository.AdicionarAsync(profissional);
        await unitOfWork.CommitAsync();
        return ProfissionalResponseDto.FromEntity(profissional);
    }

    public async Task<ProfissionalResponseDto?> AtualizarAsync(int id, SalvarProfissionalDto dto)
    {
        if (await repository.ObterPorIdAsync(id) is not { } profissional) return null;
        profissional.Atualizar(dto.Nome, dto.Funcao, dto.Email);
        await unitOfWork.CommitAsync();
        return ProfissionalResponseDto.FromEntity(profissional);
    }

    /// <summary>Os compromissos da pessoa continuam na agenda, sem profissional definido.</summary>
    public async Task<bool> ExcluirAsync(int id)
    {
        if (await repository.ObterPorIdAsync(id) is not { } profissional) return false;
        repository.Remover(profissional);
        await unitOfWork.CommitAsync();
        return true;
    }
}

public class MedicamentoAppService(IRepository<Medicamento> repository, IEstoqueRepository estoque, IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<MedicamentoResponseDto>> ListarAsync() =>
        (await repository.ObterTodosAsync()).OrderBy(m => m.Nome).Select(MedicamentoResponseDto.FromEntity).ToList();

    public async Task<MedicamentoResponseDto?> ObterAsync(int id) =>
        await repository.ObterPorIdAsync(id) is { } m ? MedicamentoResponseDto.FromEntity(m) : null;

    public async Task<MedicamentoResponseDto> CadastrarAsync(SalvarMedicamentoDto d)
    {
        var medicamento = new Medicamento(d.Nome, d.PrincipioAtivo, d.Forma, d.Unidade, d.Concentracao ?? 0, d.Fracionavel,
            d.Via, d.DoseMinCao, d.DoseMaxCao, d.DoseMinGato, d.DoseMaxGato, d.DoseMaximaMg, d.IntervaloPadraoHoras,
            d.Observacoes, d.Fonte);
        await repository.AdicionarAsync(medicamento);
        await unitOfWork.CommitAsync();
        return MedicamentoResponseDto.FromEntity(medicamento);
    }

    public async Task<MedicamentoResponseDto?> AtualizarAsync(int id, SalvarMedicamentoDto d)
    {
        if (await repository.ObterPorIdAsync(id) is not { } medicamento) return null;
        medicamento.Atualizar(d.Nome, d.PrincipioAtivo, d.Forma, d.Unidade, d.Concentracao ?? 0, d.Fracionavel,
            d.Via, d.DoseMinCao, d.DoseMaxCao, d.DoseMinGato, d.DoseMaxGato, d.DoseMaximaMg, d.IntervaloPadraoHoras,
            d.Observacoes, d.Fonte);
        await unitOfWork.CommitAsync();
        return MedicamentoResponseDto.FromEntity(medicamento);
    }

    /// <summary>
    /// As prescrições já feitas guardam os dados do medicamento e continuam na ficha.
    /// O estoque do medicamento sai junto.
    /// </summary>
    public async Task<bool> ExcluirAsync(int id)
    {
        if (await repository.ObterPorIdAsync(id) is not { } medicamento) return false;
        await estoque.RemoverItemAsync(CategoriaEstoque.Medicamento, id);
        repository.Remover(medicamento);
        await unitOfWork.CommitAsync();
        return true;
    }
}

public class RacaoAppService(IRepository<Racao> repository, IEstoqueRepository estoque, IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<RacaoResponseDto>> ListarAsync() =>
        (await repository.ObterTodosAsync()).OrderBy(r => r.Nome).Select(RacaoResponseDto.FromEntity).ToList();

    public async Task<RacaoResponseDto> CadastrarAsync(SalvarRacaoDto d)
    {
        var racao = new Racao(d.Nome, d.Especie, d.Fase, d.Tipo, d.KcalPorKg ?? 0, d.Observacoes, d.FotoUrl);
        await repository.AdicionarAsync(racao);
        await unitOfWork.CommitAsync();
        return RacaoResponseDto.FromEntity(racao);
    }

    public async Task<RacaoResponseDto?> AtualizarAsync(int id, SalvarRacaoDto d)
    {
        if (await repository.ObterPorIdAsync(id) is not { } racao) return null;
        racao.Atualizar(d.Nome, d.Especie, d.Fase, d.Tipo, d.KcalPorKg ?? 0, d.Observacoes, d.FotoUrl);
        await unitOfWork.CommitAsync();
        return RacaoResponseDto.FromEntity(racao);
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        if (await repository.ObterPorIdAsync(id) is not { } racao) return false;
        await estoque.RemoverItemAsync(CategoriaEstoque.Racao, id);
        repository.Remover(racao);
        await unitOfWork.CommitAsync();
        return true;
    }
}

public class MaterialAppService(IRepository<Material> repository, IEstoqueRepository estoque, IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<MaterialResponseDto>> ListarAsync() =>
        (await repository.ObterTodosAsync()).OrderBy(m => m.Tipo).ThenBy(m => m.Nome).Select(MaterialResponseDto.FromEntity).ToList();

    public async Task<MaterialResponseDto> CadastrarAsync(SalvarMaterialDto d)
    {
        var material = new Material(d.Nome, d.Tipo, d.Unidade, d.Especificacao, d.Observacoes);
        await repository.AdicionarAsync(material);
        await unitOfWork.CommitAsync();
        return MaterialResponseDto.FromEntity(material);
    }

    public async Task<MaterialResponseDto?> AtualizarAsync(int id, SalvarMaterialDto d)
    {
        if (await repository.ObterPorIdAsync(id) is not { } material) return null;
        material.Atualizar(d.Nome, d.Tipo, d.Unidade, d.Especificacao, d.Observacoes);
        await unitOfWork.CommitAsync();
        return MaterialResponseDto.FromEntity(material);
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        if (await repository.ObterPorIdAsync(id) is not { } material) return false;
        await estoque.RemoverItemAsync(CategoriaEstoque.Material, id);
        repository.Remover(material);
        await unitOfWork.CommitAsync();
        return true;
    }
}
