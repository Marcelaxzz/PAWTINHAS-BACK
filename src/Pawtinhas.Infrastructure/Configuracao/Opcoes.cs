using System.ComponentModel.DataAnnotations;

namespace Pawtinhas.Infrastructure.Configuracao;

/// <summary>Assinatura dos tokens JWT. A chave nunca fica no código: vem de user-secrets ou variável de ambiente.</summary>
public sealed class JwtOptions
{
    public const string Secao = "Jwt";

    [Required(ErrorMessage = "Configure Jwt:Chave (mínimo de 32 caracteres) em user-secrets ou variável de ambiente.")]
    [MinLength(32, ErrorMessage = "Jwt:Chave precisa ter pelo menos 32 caracteres.")]
    public string Chave { get; init; } = "";

    [Required] public string Emissor { get; init; } = "";
    [Required] public string Audiencia { get; init; } = "";

    [Range(1, 60)] public int MinutosAcesso { get; init; } = 15;
}

public enum ModoEmail
{
    Smtp,

    /// <summary>
    /// Por HTTPS: a API entrega o e-mail a um serviço intermediário (a função api/correio do site, na
    /// Vercel), que envia pelo Gmail da clínica. Serve para hospedagens que bloqueiam SMTP (o Railway,
    /// fora do plano Pro).
    /// </summary>
    Http,

    /// <summary>Somente desenvolvimento: grava os e-mails como arquivos .eml em vez de enviar.</summary>
    Pasta,
}

/// <summary>Envio de e-mail (códigos do login, troca de senha e resumo diário).</summary>
public sealed class EmailOptions
{
    public const string Secao = "Email";

    public ModoEmail Modo { get; init; } = ModoEmail.Smtp;

    public string Host { get; init; } = "";
    [Range(1, 65535)] public int Porta { get; init; } = 587;

    /// <summary>Auto, StartTls, SslOnConnect ou Nenhuma.</summary>
    public string Seguranca { get; init; } = "StartTls";

    public string Usuario { get; init; } = "";

    /// <summary>No Gmail, a "senha de app" (16 letras), nunca a senha da conta.</summary>
    public string Senha { get; init; } = "";

    [Required(ErrorMessage = "Configure Email:Remetente (o endereço que envia os códigos).")]
    [EmailAddress]
    public string Remetente { get; init; } = "";

    public string NomeRemetente { get; init; } = "Pawtinhas";

    /// <summary>Modo Http: endereço do serviço de envio (ex.: https://pawtinhas.vercel.app/api/correio).</summary>
    public string UrlEnvio { get; init; } = "";

    /// <summary>Modo Http: segredo que o serviço confere antes de enviar (o mesmo EMAIL_TOKEN da Vercel).</summary>
    public string TokenEnvio { get; init; } = "";

    /// <summary>Pasta usada no modo "Pasta".</summary>
    public string Pasta { get; init; } = "dados/emails";
}

public sealed class ArmazenamentoOptions
{
    public const string Secao = "Armazenamento";

    [Required] public string Pasta { get; init; } = "dados/arquivos";
    [Range(1, 20)] public int TamanhoMaximoMb { get; init; } = 5;

    /// <summary>Pasta dos backups automáticos (JSON).</summary>
    [Required] public string PastaBackups { get; init; } = "dados/backups";

    [Range(1, 365)] public int BackupsMantidos { get; init; } = 14;
}
