namespace Pawtinhas.API.Middlewares;

/// <summary>Cabeçalhos de segurança em todas as respostas; respostas da API nunca vão para cache.</summary>
public sealed class CabecalhosSegurancaMiddleware(RequestDelegate proximo)
{
    public Task InvokeAsync(HttpContext http)
    {
        var h = http.Response.Headers;
        h.XContentTypeOptions = "nosniff";
        h.XFrameOptions = "DENY";
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";
        h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
        h["Cross-Origin-Opener-Policy"] = "same-origin";

        if (http.Request.Path.StartsWithSegments("/api"))
        {
            h.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            if (!http.Request.Path.StartsWithSegments("/api/arquivos")) h.CacheControl = "no-store";
        }

        return proximo(http);
    }
}
