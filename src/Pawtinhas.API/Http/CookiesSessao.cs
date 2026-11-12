using Pawtinhas.Application.Autenticacao;

namespace Pawtinhas.API.Http;

/// <summary>
/// Os tokens da sessão viajam em cookies httpOnly + SameSite=Strict: o JavaScript da página nunca os enxerga.
/// O de acesso vale para toda a API; o de renovação só é enviado às rotas de autenticação.
/// </summary>
public sealed class CookiesSessao(IWebHostEnvironment env)
{
    public const string CookieAcesso = "pawtinhas_sessao";
    public const string CookieRenovacao = "pawtinhas_renovacao";

    private const string CaminhoApi = "/api";
    private const string CaminhoRenovacao = "/api/auth";

    public string? TokenRenovacao(HttpContext http) => http.Request.Cookies[CookieRenovacao];

    public void Gravar(HttpContext http, SessaoEmitida sessao)
    {
        http.Response.Cookies.Append(CookieAcesso, sessao.Acesso.Valor, Opcoes(http, CaminhoApi, sessao.Acesso.ExpiraEm));
        http.Response.Cookies.Append(CookieRenovacao, sessao.TokenRenovacao, Opcoes(http, CaminhoRenovacao, sessao.ExpiraRenovacao));
    }

    public void Limpar(HttpContext http)
    {
        http.Response.Cookies.Delete(CookieAcesso, Opcoes(http, CaminhoApi, null));
        http.Response.Cookies.Delete(CookieRenovacao, Opcoes(http, CaminhoRenovacao, null));
    }

    private CookieOptions Opcoes(HttpContext http, string caminho, DateTime? expira) => new()
    {
        HttpOnly = true,
        // Em produção é sempre HTTPS. Em desenvolvimento (http://localhost) segue o protocolo da requisição.
        Secure = !env.IsDevelopment() || http.Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = caminho,
        Expires = expira is { } e ? new DateTimeOffset(DateTime.SpecifyKind(e, DateTimeKind.Utc)) : null,
        IsEssential = true,
    };
}
