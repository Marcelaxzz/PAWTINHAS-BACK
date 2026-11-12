using System.Security.Cryptography;
using System.Text;
using Pawtinhas.API.Http;

namespace Pawtinhas.API.Middlewares;

/// <summary>
/// Proteção CSRF "double submit": o servidor entrega um token num cookie legível pelo
/// JavaScript do próprio site, que precisa devolvê-lo no cabeçalho X-XSRF-TOKEN em toda
/// requisição que altera dados. Outro site não consegue ler o cookie, logo não forja o cabeçalho.
/// </summary>
public sealed class ProtecaoCsrfMiddleware(RequestDelegate proximo, IWebHostEnvironment env)
{
    public const string Cookie = "XSRF-TOKEN";
    public const string Cabecalho = "X-XSRF-TOKEN";

    private static readonly HashSet<string> MetodosSeguros = ["GET", "HEAD", "OPTIONS"];

    public async Task InvokeAsync(HttpContext http)
    {
        if (!http.Request.Path.StartsWithSegments("/api"))
        {
            await proximo(http);
            return;
        }

        var tokenCookie = http.Request.Cookies[Cookie];
        if (string.IsNullOrEmpty(tokenCookie))
        {
            tokenCookie = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
            http.Response.Cookies.Append(Cookie, tokenCookie, new CookieOptions
            {
                HttpOnly = false,
                Secure = !env.IsDevelopment() || http.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                IsEssential = true,
            });
        }

        // O app mobile não lê cookies como o navegador: recebe o mesmo token também no cabeçalho.
        // Outro site não lê cabeçalhos de resposta de uma origem diferente, então a proteção continua valendo.
        http.Response.Headers[Cabecalho] = tokenCookie;

        // O ESP32 se autentica por chave própria, sem cookies.
        var isento = http.Request.Path.StartsWithSegments("/api/dispositivo");

        if (!isento && !MetodosSeguros.Contains(http.Request.Method))
        {
            var tokenCabecalho = http.Request.Headers[Cabecalho].ToString();
            if (!Iguais(tokenCabecalho, http.Request.Cookies[Cookie]))
            {
                await Erros.EscreverAsync(http, StatusCodes.Status403Forbidden,
                    "Sua sessão de segurança expirou. Recarregue a página e tente de novo.", "csrf");
                return;
            }
        }

        await proximo(http);
    }

    private static bool Iguais(string? a, string? b) =>
        !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b)
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
