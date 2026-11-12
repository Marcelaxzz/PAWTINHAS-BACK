using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.JsonWebTokens;
using Pawtinhas.API.Configuracao;
using Pawtinhas.API.Http;
using Pawtinhas.API.Middlewares;
using Pawtinhas.Application.Autenticacao;
using Pawtinhas.Application.Comum;

namespace Pawtinhas.API.Controllers;

/// <summary>
/// Login em duas etapas: e-mail e senha, depois o código de 6 dígitos enviado por e-mail.
/// Respostas genéricas: nenhuma rota revela qual e-mail tem acesso.
/// </summary>
[ApiController]
[Route("api/auth")]
[EnableRateLimiting(LimitesRequisicao.Autenticacao)]
public sealed class AuthController(
    ServicoAutenticacao autenticacao,
    ServicoSessoes sessoes,
    CookiesSessao cookies,
    IMemoryCache cache) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<DesafioResposta>> Login(LoginRequisicao req) =>
        Ok(await autenticacao.EntrarAsync(req));

    [HttpPost("login/verify")]
    [AllowAnonymous]
    public async Task<ActionResult<SessaoResposta>> ConfirmarLogin(CodigoRequisicao req) =>
        AbrirSessao(await autenticacao.ConfirmarLoginAsync(req));

    [HttpPost("code/resend")]
    [AllowAnonymous]
    public async Task<ActionResult<ReenvioResposta>> ReenviarCodigo(ReenvioRequisicao req) =>
        Ok(await autenticacao.ReenviarCodigoAsync(req));

    /// <summary>Pede a troca de senha. A nova senha só vale depois que o código do e-mail for confirmado.</summary>
    [HttpPost("password/forgot")]
    [AllowAnonymous]
    public async Task<ActionResult<DesafioResposta>> EsqueciSenha(RedefinicaoRequisicao req) =>
        Ok(await autenticacao.SolicitarRedefinicaoAsync(req));

    [HttpPost("password/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmarNovaSenha(CodigoRequisicao req)
    {
        await autenticacao.ConfirmarRedefinicaoAsync(req);
        return Ok(new { ok = true });
    }

    [HttpPost("refresh")]
    [EnableRateLimiting(LimitesRequisicao.Sessao)]
    [AllowAnonymous]
    public async Task<ActionResult<SessaoResposta>> Renovar()
    {
        var sessao = await sessoes.RenovarAsync(cookies.TokenRenovacao(HttpContext));
        if (sessao is null)
        {
            cookies.Limpar(HttpContext);
            throw AutenticacaoException.SessaoExpirada();
        }
        return AbrirSessao(sessao);
    }

    [HttpPost("logout")]
    [EnableRateLimiting(LimitesRequisicao.Sessao)]
    [AllowAnonymous]
    public async Task<IActionResult> Sair()
    {
        await sessoes.EncerrarAsync(cookies.TokenRenovacao(HttpContext));
        if (Guid.TryParse(User.FindFirst(Claims.Sessao)?.Value, out var sessao)) SessaoAtivaMiddleware.Invalidar(cache, sessao);
        cookies.Limpar(HttpContext);
        return Ok(new { ok = true });
    }

    /// <summary>
    /// Quem está logado. Visitante sem nenhum cookie de sessão recebe { user: null } (sem 401 a cada
    /// página pública). Com o cookie de renovação, um acesso vencido responde 401 para o site renovar.
    /// </summary>
    [HttpGet("me")]
    [AllowAnonymous]
    [EnableRateLimiting(LimitesRequisicao.Sessao)]
    public async Task<ActionResult<SessaoResposta>> Eu()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            if (cookies.TokenRenovacao(HttpContext) is null) return Ok(new SessaoResposta(null));
            throw AutenticacaoException.SessaoExpirada();
        }

        if (!int.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id))
            throw AutenticacaoException.SessaoExpirada();
        return Ok(new SessaoResposta(await autenticacao.ObterUsuarioAsync(id)));
    }

    private OkObjectResult AbrirSessao(SessaoEmitida sessao)
    {
        cookies.Gravar(HttpContext, sessao);
        return Ok(new SessaoResposta(UsuarioResposta.De(sessao.Usuario)));
    }
}
