using System.ComponentModel.DataAnnotations;

namespace Pawtinhas.Application.Configuracao;

/// <summary>Regras do login em duas etapas (seção "Autenticacao").</summary>
public sealed class AutenticacaoOptions
{
    public const string Secao = "Autenticacao";

    /// <summary>Único e-mail que pode entrar no painel. Qualquer outro recebe a mesma resposta de "e-mail ou senha incorretos".</summary>
    [Required(ErrorMessage = "Configure Autenticacao:EmailPermitido (o e-mail da veterinária).")]
    [EmailAddress]
    public string EmailPermitido { get; init; } = "";

    [Range(1, 60)] public int MinutosValidadeCodigo { get; init; } = 10;
    [Range(1, 10)] public int TentativasPorCodigo { get; init; } = 5;
    [Range(10, 600)] public int SegundosEntreReenvios { get; init; } = 60;
    [Range(1, 10)] public int MaximoReenvios { get; init; } = 5;
    [Range(1, 20)] public int FalhasAteBloqueio { get; init; } = 5;
    [Range(1, 1440)] public int MinutosBloqueio { get; init; } = 15;
    [Range(1, 72)] public int HorasSessao { get; init; } = 12;
    [Range(1, 90)] public int DiasLembrarSessao { get; init; } = 30;
}

/// <summary>Integração com o alarme ESP32 (seção "Dispositivo").</summary>
public sealed class DispositivoOptions
{
    public const string Secao = "Dispositivo";

    /// <summary>Chave enviada pelo ESP32 no cabeçalho X-Device-Key. Vazia desliga a integração.</summary>
    public string Chave { get; init; } = "";

    /// <summary>Porta UDP em que a API responde ao ESP32 que procura por ela na rede. 0 desliga.</summary>
    public int PortaDescoberta { get; init; } = 5199;
}

/// <summary>Endereço público do site, usado nos links dos e-mails (seção "Frontend").</summary>
public sealed class FrontendOptions
{
    public const string Secao = "Frontend";

    public string Url { get; init; } = "";
}
