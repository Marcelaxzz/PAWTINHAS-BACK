using Pawtinhas.Application.DTOs;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Comum;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Services;

/// <summary>Alarmes (ESP32, app e painel) e o histórico de disparos.</summary>
public class AlarmeAppService(IAlarmeRepository repository, IAnimalRepository animais, IUnitOfWork unitOfWork, TimeProvider relogio)
{
    public async Task<IReadOnlyList<AlarmeResponseDto>> ListarAsync() =>
        (await repository.ObterTodosAsync()).OrderBy(a => a.Hora).Select(AlarmeResponseDto.FromEntity).ToList();

    public async Task<AlarmeResponseDto?> ObterAsync(int id) =>
        await repository.ObterPorIdAsync(id) is { } a ? AlarmeResponseDto.FromEntity(a) : null;

    public async Task<AlarmeResponseDto> CadastrarAsync(SalvarAlarmeDto dto)
    {
        await ValidarAnimalAsync(dto.AnimalId);
        var inicio = dto.Inicio ?? DateOnly.FromDateTime(relogio.GetLocalNow().DateTime);
        var alarme = new Alarme(dto.Nome, dto.AnimalId, dto.Descricao, dto.Hora!.Value, dto.Recorrencia, inicio, dto.Ativo);
        await repository.AdicionarAsync(alarme);
        await unitOfWork.CommitAsync();
        return (await ObterAsync(alarme.Id))!;
    }

    public async Task<AlarmeResponseDto?> AtualizarAsync(int id, SalvarAlarmeDto dto)
    {
        if (await repository.ObterPorIdAsync(id) is not { } alarme) return null;
        await ValidarAnimalAsync(dto.AnimalId);
        alarme.Atualizar(dto.Nome, dto.AnimalId, dto.Descricao, dto.Hora!.Value, dto.Recorrencia, dto.Inicio ?? alarme.Inicio, dto.Ativo);
        await unitOfWork.CommitAsync();
        return await ObterAsync(id);
    }

    public async Task<AlarmeResponseDto?> AlterarAtivoAsync(int id, bool ativo)
    {
        if (await repository.ObterPorIdAsync(id) is not { } alarme) return null;
        alarme.DefinirAtivo(ativo);
        await unitOfWork.CommitAsync();
        return AlarmeResponseDto.FromEntity(alarme);
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        if (await repository.ObterPorIdAsync(id) is not { } alarme) return false;
        repository.Remover(alarme);
        await unitOfWork.CommitAsync();
        return true;
    }

    public async Task<IReadOnlyList<DisparoAlarmeResponseDto>> HistoricoAsync() =>
        (await repository.UltimosDisparosAsync(200)).Select(DisparoAlarmeResponseDto.FromEntity).ToList();

    private async Task ValidarAnimalAsync(int? animalId)
    {
        if (animalId is { } id && await animais.ObterPorIdAsync(id) is not { EhPaciente: true })
            throw new DominioException("animalId", "Paciente não encontrado. Selecione outro.");
    }
}

