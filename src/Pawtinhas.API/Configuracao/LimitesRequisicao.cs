using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Pawtinhas.API.Http;

namespace Pawtinhas.API.Configuracao;

/// <summary>Limite de requisições por IP. As rotas de login têm um limite mais rígido.</summary>
public sealed class LimitesRequisicao
{
    public const string Secao = "LimiteRequisicoes";
    public const string Autenticacao = "autenticacao";

    /// <summary>Renovação, saída e "me": tokens aleatórios de 256 bits não precisam do limite rígido do login.</summary>
    public const string Sessao = "sessao";

    public int AutenticacaoPorMinuto { get; init; } = 10;
    public int GeralPorMinuto { get; init; } = 600;

    public static void Configurar(RateLimiterOptions o, LimitesRequisicao limites)
    {
        o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        o.OnRejected = async (contexto, ct) =>
        {
            if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                contexto.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(espera.TotalSeconds)).ToString();
            await Erros.EscreverAsync(contexto.HttpContext, StatusCodes.Status429TooManyRequests,
                "Muitas tentativas. Aguarde um pouco e tente novamente.", "limite_requisicoes");
        };

        o.AddPolicy(Autenticacao, http => Janela(http, limites.AutenticacaoPorMinuto));
        o.AddPolicy(Sessao, http => Janela(http, limites.GeralPorMinuto));
        o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http => Janela(http, limites.GeralPorMinuto));
    }

    private static RateLimitPartition<string> Janela(HttpContext http, int porMinuto) =>
        RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = porMinuto, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
}

/// <summary>Origens liberadas no CORS (só quando o site fica em outro domínio que a API).</summary>
public sealed class CorsOptions
{
    public const string Secao = "Cors";

    public string[] Origens { get; init; } = [];
}

/// <summary>
/// Hospedagem atrás de Nginx, Caddy ou do balanceador da hospedagem. Sem isto, a API enxerga o IP
/// do proxy em todas as requisições: o limite por IP passaria a valer para a clínica inteira e o
/// redirecionamento para HTTPS entraria em laço. Só os endereços listados podem informar o IP real.
/// </summary>
public sealed class ProxyReversoOptions
{
    public const string Secao = "ProxyReverso";

    /// <summary>
    /// IPs dos proxies confiáveis (ex.: "127.0.0.1"). Vazio = API exposta direto.
    /// ["*"] = atrás do proxy de uma plataforma sem IP fixo (Railway), sem porta pública própria.
    /// </summary>
    public string[] Enderecos { get; init; } = [];

    /// <summary>
    /// Quantos proxies ficam na frente da API. 1 = só o do Railway; 2 = a Vercel repassa /api para o
    /// Railway, e o IP do visitante é o que a Vercel anotou, um salto antes.
    /// </summary>
    public int Saltos { get; init; } = 1;

    public static ForwardedHeadersOptions? Opcoes(IConfiguration config)
    {
        var proxy = config.GetSection(Secao).Get<ProxyReversoOptions>();
        var enderecos = proxy?.Enderecos ?? [];
        if (enderecos.Length == 0) return null;

        var opcoes = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = Math.Clamp(proxy?.Saltos ?? 1, 1, 3),
        };
        opcoes.KnownProxies.Clear();
        opcoes.KnownIPNetworks.Clear();
        // "*": a API só é alcançada pelo proxy da hospedagem (Railway), que não tem IP fixo.
        // Com as duas listas vazias, o cabeçalho é aceito de qualquer origem e ForwardLimit (Saltos)
        // limita quantos saltos do fim da lista são lidos.
        if (enderecos is ["*"]) return opcoes;
        foreach (var endereco in enderecos)
        {
            if (!IPAddress.TryParse(endereco, out var ip))
                throw new InvalidOperationException($"ProxyReverso:Enderecos tem um IP inválido: {endereco}");
            opcoes.KnownProxies.Add(ip);
        }
        return opcoes;
    }
}
