using Pawtinhas.Domain;

namespace Pawtinhas.Domain.Interfaces;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> ObterPorEmailAsync(string email);
    
    Task<IReadOnlyList<Usuario>> ListarTutoresAsync();

    Task<Usuario?> ObterPorResponsavelAsync(int responsavelId);
}

public interface IDesafioCodigoRepository
{
    Task AdicionarAsync(DesafioCodigo desafio);

    Task<DesafioCodigo?> ObterAsync(Guid id, FinalidadeCodigo finalidade);

    Task<DesafioCodigo?> ObterAsync(Guid id);

    Task<int> ContarDesdeAsync(int usuarioId, FinalidadeCodigo finalidade, DateTime desde);

    void Remover(DesafioCodigo desafio);

    /// <summary>Apaga desafios vencidos ou usados há mais de um dia. Retorna quantos saíram.</summary>
    Task<int> LimparAsync(DateTime agora);
}

public interface ISessaoRepository
{
    Task AdicionarAsync(SessaoUsuario sessao);

    Task<SessaoUsuario?> ObterPorHashAsync(string tokenHash);

    /// <summary>A família (login) ainda tem um token válido? Usado pelo middleware de sessão.</summary>
    Task<bool> FamiliaAtivaAsync(Guid familia, DateTime agora);

    Task RevogarFamiliaAsync(Guid familia, DateTime agora);

    Task RevogarTodasAsync(int usuarioId, DateTime agora);

    Task<int> LimparAsync(DateTime agora);
}
