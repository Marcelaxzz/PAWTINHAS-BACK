
using Microsoft.EntityFrameworkCore;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Enums;
using Pawtinhas.Domain.Interfaces;
using Pawtinhas.Infrastructure.Data;

namespace Pawtinhas.Infrastructure.Repositories;

public class AnimalRepository : RepositoryBase<Animal>, IAnimalRepository
{
    public AnimalRepository(AppDbContext context) : base(context)
    {
    }

    public override Task<Animal?> ObterPorIdAsync(int id) =>
        _dbSet.Include(a => a.Responsavel).FirstOrDefaultAsync(a => a.Id == id);

    public override async Task<IReadOnlyList<Animal>> ObterTodosAsync() =>
        await _dbSet.Include(a => a.Responsavel).OrderBy(a => a.Nome).ToListAsync();

    public async Task<IReadOnlyList<Animal>> ListarPacientesAsync() =>
        await _dbSet.Include(a => a.Responsavel)
            .Where(a => a.ResponsavelId != null)
            .OrderBy(a => a.Nome)
            .ToListAsync();

    public async Task<IReadOnlyList<Animal>> ListarAdocaoAsync(bool somentePublicos)
    {
        var consulta = _dbSet.Include(a => a.PerfilAdocao).Where(a => a.PerfilAdocao != null);
        if (somentePublicos) consulta = consulta.Where(a => a.Status != SituacaoAnimal.ADOTADO);
        return await consulta
            .OrderByDescending(a => a.PerfilAdocao!.DataEntrada)
            .ThenBy(a => a.Nome)
            .ToListAsync();
    }

    public Task<Animal?> ObterComPerfilAsync(int id) =>
        _dbSet.Include(a => a.PerfilAdocao).Include(a => a.Responsavel).FirstOrDefaultAsync(a => a.Id == id);
}
