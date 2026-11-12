using Microsoft.Extensions.Caching.Memory;
using Pawtinhas.API.Http;
using Pawtinhas.Application.Autenticacao;

namespace Pawtinhas.API.Middlewares;

/// <summary>
/// Login por middleware: depois da validação do JWT (assinatura, emissor, validade), confere se a
/// sessão dele ainda existe no banco. O logout derruba o acesso na hora e a troca de senha em até
/// 20 segundos, sem esperar os 15 minutos do token. O resultado fica nesse cache curto para não
/// consultar o banco a cada requisição.
/// </summary>
public sealed class SessaoAtivaMiddleware(RequestDelegate proximo, IMemoryCache cache)
{
    private static readonly TimeSpan Cache = TimeSpan.FromSeconds(20);

    public async Task InvokeAsync(HttpContext http, ServicoSessoes sessoes, CookiesSessao cookies)
    {
        // Sair sempre funciona, mesmo com a sessão já encerrada.
        if (http.User.Identity?.IsAuthenticated != true || http.Request.Path.StartsWithSegments("/api/auth/logout"))
        {
            await proximo(http);
            return;
        }

        if (!Guid.TryParse(http.User.FindFirst(Claims.Sessao)?.Value, out var sessao)
            || !await cache.GetOrCreateAsync($"sessao:{sessao}", entrada =>
            {
                entrada.AbsoluteExpirationRelativeToNow = Cache;
                return sessoes.AtivaAsync(sessao);
            }))
        {
            cookies.Limpar(http);
            await Erros.EscreverAsync(http, StatusCodes.Status401Unauthorized, "Sua sessão foi encerrada. Entre novamente.", "sessao_encerrada");
            return;
        }

        await proximo(http);
    }

    /// <summary>Esquece o resultado em cache (logout e troca de senha valem na hora).</summary>
    public static void Invalidar(IMemoryCache cache, Guid sessao) => cache.Remove($"sessao:{sessao}");
}
