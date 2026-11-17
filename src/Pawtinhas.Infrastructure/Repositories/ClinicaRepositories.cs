using Microsoft.EntityFrameworkCore;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Interfaces;
using Pawtinhas.Infrastructure.Data;

namespace Pawtinhas.Infrastructure.Repositories;

public class AlarmeRepository(AppDbContext context) : RepositoryBase<Alarme>(context), IAlarmeRepository
{
    public override Task<Alarme?> ObterPorIdAsync(int id) =>
        _dbSet.Include(a => a.Animal).FirstOrDefaultAsync(a => a.Id == id);

    public override async Task<IReadOnlyList<Alarme>> ObterTodosAsync() =>
        await _dbSet.Include(a => a.Animal).ToListAsync();

    public async Task<IReadOnlyList<Alarme>> ListarAtivosAsync() =>
        await _dbSet.AsNoTracking().Include(a => a.Animal).Where(a => a.Ativo).OrderBy(a => a.Hora).ToListAsync();

    public async Task<IReadOnlyList<DisparoAlarme>> UltimosDisparosAsync(int quantidade) =>
        await _context.DisparosAlarme.AsNoTracking().OrderByDescending(d => d.Quando).Take(quantidade).ToListAsync();

    public async Task AdicionarDisparoAsync(DisparoAlarme disparo) => await _context.DisparosAlarme.AddAsync(disparo);
}

public class PrescricaoRepository(AppDbContext context) : RepositoryBase<Prescricao>(context), IPrescricaoRepository
{
    public async Task<IReadOnlyList<Prescricao>> ListarAsync(int? animalId) =>
        await _dbSet.AsNoTracking()
            .Where(p => animalId == null || p.AnimalId == animalId)
            .OrderByDescending(p => p.CriadoEm)
            .ToListAsync();
}

public class PlanoAlimentarRepository(AppDbContext context) : RepositoryBase<PlanoAlimentar>(context), IPlanoAlimentarRepository
{
    public async Task<IReadOnlyList<PlanoAlimentar>> ListarAsync(int? animalId) =>
        await _dbSet.AsNoTracking()
            .Where(p => animalId == null || p.AnimalId == animalId)
            .OrderByDescending(p => p.CriadoEm)
            .ToListAsync();
}

public class ConfiguracaoRepository(AppDbContext context, TimeProvider relogio) : IConfiguracaoRepository
{
    public async Task<ConfiguracaoClinica> ObterAsync()
    {
        var config = await context.Configuracoes.OrderBy(c => c.Id).FirstOrDefaultAsync();
        if (config is not null) return config;

        config = ConfiguracaoClinica.Padrao(relogio.GetLocalNow().DateTime);
        context.Configuracoes.Add(config);
        await context.SaveChangesAsync();
        return config;
    }
}

public class EstoqueRepository(AppDbContext context) : IEstoqueRepository
{
    public async Task<IReadOnlyList<ItemEstoque>> ListarAsync() =>
        await context.ItensEstoque.AsNoTracking().Include(i => i.Lotes).ToListAsync();

    public Task<ItemEstoque?> ObterAsync(string categoria, int itemId) =>
        context.ItensEstoque.Include(i => i.Lotes).FirstOrDefaultAsync(i => i.Categoria == categoria && i.ItemId == itemId);

    public async Task AdicionarAsync(ItemEstoque item) => await context.ItensEstoque.AddAsync(item);

    public async Task AdicionarMovimentacaoAsync(MovimentacaoEstoque movimentacao) =>
        await context.MovimentacoesEstoque.AddAsync(movimentacao);

    public async Task<IReadOnlyList<MovimentacaoEstoque>> MovimentacoesAsync(
        string? categoria, int? itemId, DateTime? de, DateTime? ate, int limite) =>
        await context.MovimentacoesEstoque.AsNoTracking()
            .Include(m => m.Item)
            .Where(m => categoria == null || m.Item!.Categoria == categoria)
            .Where(m => itemId == null || m.Item!.ItemId == itemId)
            .Where(m => de == null || m.CriadoEm >= de)
            .Where(m => ate == null || m.CriadoEm < ate)
            .OrderByDescending(m => m.CriadoEm)
            .ThenByDescending(m => m.Id)
            .Take(limite)
            .ToListAsync();

    public async Task RemoverItemAsync(string categoria, int itemId)
    {
        if (await context.ItensEstoque.FirstOrDefaultAsync(i => i.Categoria == categoria && i.ItemId == itemId) is { } item)
            context.ItensEstoque.Remove(item);
    }
}

public class RelatorioServicoRepository(AppDbContext context) : IRelatorioServicoRepository
{
    public Task<RelatorioServico?> ObterPorEventoAsync(int eventoId) =>
        context.RelatoriosServico.FirstOrDefaultAsync(r => r.EventoId == eventoId);

    public async Task AdicionarAsync(RelatorioServico relatorio) => await context.RelatoriosServico.AddAsync(relatorio);

    public async Task<IReadOnlyList<int>> EventosComRelatorioAsync() =>
        await context.RelatoriosServico.AsNoTracking().Select(r => r.EventoId).ToListAsync();
}
