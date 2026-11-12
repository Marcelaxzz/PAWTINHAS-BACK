namespace Pawtinhas.Domain;

public enum FinalidadeCodigo
{
    Login = 1,
    RedefinicaoSenha = 2,
}

/// <summary>
/// Segunda etapa do login (e da troca de senha): código de 6 dígitos enviado por e-mail.
/// Só o HMAC do código é guardado. O Id é aleatório (Guid) porque vai para o navegador.
/// </summary>
public class DesafioCodigo
{
    public Guid Id { get; private set; }
    public FinalidadeCodigo Finalidade { get; private set; }

    /// <summary>Nulo quando o e-mail informado não pode entrar: o desafio nunca será aceito, mas a resposta é idêntica.</summary>
    public int? UsuarioId { get; private set; }

    public string CodigoHash { get; private set; } = string.Empty;
    public int Tentativas { get; private set; }
    public int Reenvios { get; private set; }
    public bool LembrarSessao { get; private set; }

    /// <summary>Hash da nova senha, aplicado só depois que o código for confirmado.</summary>
    public string? SenhaPendenteHash { get; private set; }

    public DateTime CriadoEm { get; private set; }
    public DateTime ExpiraEm { get; private set; }
    public DateTime ReenvioPermitidoEm { get; private set; }
    public DateTime? UsadoEm { get; private set; }

    private DesafioCodigo() { }

    public DesafioCodigo(
        FinalidadeCodigo finalidade, int? usuarioId, bool lembrarSessao, string? senhaPendenteHash,
        DateTime agora, TimeSpan validade, TimeSpan esperaReenvio)
    {
        Id = Guid.NewGuid();
        Finalidade = finalidade;
        UsuarioId = usuarioId;
        LembrarSessao = lembrarSessao;
        SenhaPendenteHash = usuarioId is null ? null : senhaPendenteHash;
        CriadoEm = agora;
        ExpiraEm = agora + validade;
        ReenvioPermitidoEm = agora + esperaReenvio;
    }

    public bool Valido(DateTime agora) => UsadoEm is null && ExpiraEm > agora;

    public void DefinirCodigo(string codigoHash) => CodigoHash = codigoHash;

    public void RegistrarTentativaErrada() => Tentativas++;

    public void Reenviar(string codigoHash, DateTime agora, TimeSpan validade, TimeSpan esperaReenvio)
    {
        CodigoHash = codigoHash;
        Tentativas = 0;
        Reenvios++;
        ExpiraEm = agora + validade;
        ReenvioPermitidoEm = agora + esperaReenvio;
    }

    public void MarcarUsado(DateTime agora) => UsadoEm = agora;
}
