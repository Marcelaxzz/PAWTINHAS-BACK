namespace Pawtinhas.Domain;

/// <summary>
/// Token de renovação (refresh) com rotação: cada uso gera um novo e revoga o anterior.
/// Todos os tokens de um mesmo login formam uma família, que também identifica a sessão (claim "sid")
/// e é conferida a cada requisição pelo middleware de sessão.
/// </summary>
public class SessaoUsuario
{
    public Guid Id { get; private set; }
    public int UsuarioId { get; private set; }

    /// <summary>SHA-256 do token. O valor em si só existe no cookie do navegador.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    public Guid Familia { get; private set; }
    public bool Persistente { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime ExpiraEm { get; private set; }
    public DateTime? RevogadoEm { get; private set; }
    public Guid? SubstituidoPor { get; private set; }

    private SessaoUsuario() { }

    public SessaoUsuario(int usuarioId, string tokenHash, Guid familia, bool persistente, DateTime agora, DateTime expiraEm)
    {
        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        TokenHash = tokenHash;
        Familia = familia;
        Persistente = persistente;
        CriadoEm = agora;
        ExpiraEm = expiraEm;
    }

    public void SubstituirPor(SessaoUsuario nova, DateTime agora)
    {
        // Um token já trocado mantém a data original: a tolerância não se renova a cada uso.
        RevogadoEm ??= agora;
        SubstituidoPor ??= nova.Id;
    }
}
