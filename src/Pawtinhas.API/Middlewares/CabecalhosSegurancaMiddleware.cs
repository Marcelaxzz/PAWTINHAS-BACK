namespace Pawtinhas.API.Middlewares;

/// <summary>Cabeçalhos de segurança em todas as respostas; respostas da API nunca vão para cache.</summary>
public sealed class CabecalhosSegurancaMiddleware(RequestDelegate proximo)
{
    /// <summary>Prefixo do worker de visão computacional no build do frontend (assets/visao-worker-[hash].js).</summary>
    private const string PrefixoWorkerVisao = "/assets/visao-worker-";

    public Task InvokeAsync(HttpContext http)
    {
        var h = http.Response.Headers;
        h.XContentTypeOptions = "nosniff";
        h.XFrameOptions = "DENY";
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";
        h["Permissions-Policy"] = "camera=(self), microphone=(), geolocation=(), payment=()";
        h["Cross-Origin-Opener-Policy"] = "same-origin";

        if (http.Request.Path.StartsWithSegments("/api"))
        {
            h.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            if (!http.Request.Path.StartsWithSegments("/api/arquivos")) h.CacheControl = "no-store";
        }
        else if (http.Request.Path.Value?.StartsWith(PrefixoWorkerVisao, StringComparison.Ordinal) == true)
        {
            // O OpenCV.js (WebAssembly + embind) precisa compilar código em tempo de execução. Isso fica
            // restrito a este worker: ele não acessa a página, os cookies nem a rede, só recebe pixels.
            h.ContentSecurityPolicy =
                "default-src 'none'; script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval'; connect-src 'none'";
        }

        return proximo(http);
    }
}
