using Pawtinhas.Domain.Enums;
namespace Pawtinhas.Domain;

/// <summary>
/// Conta de acesso ao painel. A plataforma aceita um único e-mail, definido na configuração
/// (Autenticacao:EmailPermitido); a conta é criada pelo comando "usuario".
/// </summary>
public class Usuario
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public PapelUsuario Papel { get; private set; }
    public int? ResponsavelId { get; private set; }

    /// <summary>Hash PBKDF2 (ASP.NET Core Identity).</summary>
    public string SenhaHash { get; private set; } = string.Empty;

    public int FalhasLogin { get; private set; }
    public DateTime? BloqueadoAte { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private Usuario() { }

    public Usuario(string nome, string email, string senhaHash, DateTime agora)
    {
        Papel = PapelUsuario.VETERINARIA;
        Nome = nome.Trim();
        Email = NormalizarEmail(email);
        SenhaHash = senhaHash;
        CriadoEm = agora;
        AtualizadoEm = agora;
    }

    public static Usuario NovoTutor(string nome, string email, string senhaHash, int responsavelId, DateTime agora) =>
        new(nome, email, senhaHash, agora) { Papel = PapelUsuario.TUTOR, ResponsavelId = responsavelId };

    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();

    public bool Bloqueado(DateTime agora) => BloqueadoAte > agora;

    /// <summary>Conta a falha; ao chegar no limite, bloqueia pelo tempo configurado.</summary>
    public void RegistrarFalha(DateTime agora, int limite, TimeSpan bloqueio)
    {
        FalhasLogin++;
        if (FalhasLogin < limite) return;
        FalhasLogin = 0;
        BloqueadoAte = agora + bloqueio;
    }

    public void RegistrarSucesso()
    {
        FalhasLogin = 0;
        BloqueadoAte = null;
    }

    public void DefinirSenha(string senhaHash, DateTime agora)
    {
        SenhaHash = senhaHash;
        RegistrarSucesso();
        AtualizadoEm = agora;
    }

    public void AtualizarNome(string nome, DateTime agora)
    {
        Nome = nome.Trim();
        AtualizadoEm = agora;
    }
}
