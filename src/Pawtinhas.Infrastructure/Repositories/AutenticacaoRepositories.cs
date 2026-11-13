using Microsoft.EntityFrameworkCore;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Interfaces;
using Pawtinhas.Infrastructure.Data;
using Pawtinhas.Domain.Enums;

namespace Pawtinhas.Infrastructure.Repositories;

public class UsuarioRepository(AppDbContext context) : RepositoryBase<Usuario>(context), IUsuarioRepository
{
    public Task<Usuario?> ObterPorEmailAsync(string email) => _dbSet.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<IReadOnlyList<Usuario>> ListarTutoresAsync() =>
    await _dbSet.Where(u => u.Papel == PapelUsuario.TUTOR).OrderBy(u => u.Nome).ToListAsync();

    public Task<Usuario?> ObterPorResponsavelAsync(int responsavelId) =>
        _dbSet.FirstOrDefaultAsync(u => u.ResponsavelId == responsavelId);
}

public class DesafioCodigoRepository(AppDbContext context) : IDesafioCodigoRepository
{
    public async Task AdicionarAsync(DesafioCodigo desafio) => await context.DesafiosCodigo.AddAsync(desafio);

    public Task<DesafioCodigo?> ObterAsync(Guid id, FinalidadeCodigo finalidade) =>
        context.DesafiosCodigo.FirstOrDefaultAsync(d => d.Id == id && d.Finalidade == finalidade);

    public Task<DesafioCodigo?> ObterAsync(Guid id) => context.DesafiosCodigo.FirstOrDefaultAsync(d => d.Id == id);

    public Task<int> ContarDesdeAsync(int usuarioId, FinalidadeCodigo finalidade, DateTime desde) =>
        context.DesafiosCodigo.CountAsync(d => d.UsuarioId == usuarioId && d.Finalidade == finalidade && d.CriadoEm > desde);

    public void Remover(DesafioCodigo desafio) => context.DesafiosCodigo.Remove(desafio);

    public Task<int> LimparAsync(DateTime agora)
    {
        var limite = agora.AddDays(-1);
        return context.DesafiosCodigo.Where(d => d.ExpiraEm < limite).ExecuteDeleteAsync();
    }
}

public class SessaoRepository(AppDbContext context) : ISessaoRepository
{
    public async Task AdicionarAsync(SessaoUsuario sessao) => await context.Sessoes.AddAsync(sessao);

    public Task<SessaoUsuario?> ObterPorHashAsync(string tokenHash) =>
        context.Sessoes.FirstOrDefaultAsync(s => s.TokenHash == tokenHash);

    public Task<bool> FamiliaAtivaAsync(Guid familia, DateTime agora) =>
        context.Sessoes.AnyAsync(s => s.Familia == familia && s.RevogadoEm == null && s.ExpiraEm > agora);

    public Task RevogarFamiliaAsync(Guid familia, DateTime agora) =>
        context.Sessoes.Where(s => s.Familia == familia && s.RevogadoEm == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevogadoEm, agora));

    public Task RevogarTodasAsync(int usuarioId, DateTime agora) =>
        context.Sessoes.Where(s => s.UsuarioId == usuarioId && s.RevogadoEm == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevogadoEm, agora));

    public Task<int> LimparAsync(DateTime agora)
    {
        var limite = agora.AddDays(-7);
        return context.Sessoes.Where(s => s.ExpiraEm < limite).ExecuteDeleteAsync();
    }
}
