
using Microsoft.EntityFrameworkCore;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Interfaces;
using Pawtinhas.Infrastructure.Data;

namespace Pawtinhas.Infrastructure.Repositories;

public class ResponsavelRepository
    : RepositoryBase<Responsavel>, IResponsavelRepository
{
    public ResponsavelRepository(AppDbContext context)
        : base(context)
    {
    }

    public override Task<Responsavel?> ObterPorIdAsync(int id) =>
        _dbSet.Include(r => r.Animais).FirstOrDefaultAsync(r => r.Id == id);

    public override async Task<IReadOnlyList<Responsavel>> ObterTodosAsync() =>
        await _dbSet.Include(r => r.Animais).ToListAsync();

    public Task<bool> CpfEmUsoAsync(string cpf, int? ignorarId) =>
        _dbSet.AnyAsync(r => r.CPF == cpf && (ignorarId == null || r.Id != ignorarId));

    public Task<bool> TemAnimaisAsync(int id) => _context.Animais.AnyAsync(a => a.ResponsavelId == id);
}
