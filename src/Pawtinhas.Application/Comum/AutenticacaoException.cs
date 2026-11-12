namespace Pawtinhas.Application.Comum;

/// <summary>
/// Falha de autenticação com o status HTTP que ela deve gerar (401, 410, 429).
/// <see cref="RepetirApos"/> vira o cabeçalho Retry-After.
/// </summary>
public sealed class AutenticacaoException(int status, string codigo, string mensagem, TimeSpan? repetirApos = null)
    : Exception(mensagem)
{
    public int Status { get; } = status;
    public string Codigo { get; } = codigo;
    public TimeSpan? RepetirApos { get; } = repetirApos;

    public static AutenticacaoException SessaoExpirada() =>
        new(401, "sessao_expirada", "Sua sessão expirou. Entre novamente.");

    public static AutenticacaoException CodigoExpirado() =>
        new(410, "codigo_expirado", "Este código expirou. Volte e solicite um novo.");
}
