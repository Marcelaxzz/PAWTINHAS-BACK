using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Pawtinhas.Application.Abstracoes;
using Pawtinhas.Application.Configuracao;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Autenticacao;

/// <summary>
/// Sessão = token de acesso (curto) + token de renovação (longo, guardado só como hash).
/// A API grava os dois em cookies httpOnly/SameSite=Strict: o JavaScript da página nunca enxerga os tokens.
/// </summary>
public sealed class ServicoSessoes(
    ISessaoRepository sessoes,
    IUsuarioRepository usuarios,
    IUnitOfWork unitOfWork,
    IGeradorTokenAcesso tokensAcesso,
    IOptions<AutenticacaoOptions> opcoes,
    TimeProvider relogio)
{
    /// <summary>Janela em que um token recém-trocado ainda é aceito (duas abas renovando ao mesmo tempo).</summary>
    private static readonly TimeSpan ToleranciaRotacao = TimeSpan.FromSeconds(30);

    private readonly AutenticacaoOptions _cfg = opcoes.Value;

    private DateTime Agora => relogio.GetUtcNow().UtcDateTime;

    /// <summary>Cria uma sessão nova (login).</summary>
    public async Task<SessaoEmitida> IniciarAsync(Usuario usuario, bool lembrar)
    {
        var (token, registro) = NovoToken(usuario.Id, Guid.NewGuid(), lembrar, Agora);
        await sessoes.AdicionarAsync(registro);
        await unitOfWork.CommitAsync();
        return Emitir(usuario, token, registro);
    }

    /// <summary>
    /// Troca o token de renovação por um novo (rotação). Se um token já revogado for usado,
    /// alguém pode ter copiado o cookie: a família inteira é revogada. A exceção é um token trocado
    /// há poucos segundos numa sessão ainda ativa: é outra aba que pediu a renovação junto.
    /// </summary>
    /// <returns>Nulo quando não há sessão válida para renovar.</returns>
    public async Task<SessaoEmitida?> RenovarAsync(string? tokenRenovacao)
    {
        if (string.IsNullOrEmpty(tokenRenovacao)) return null;

        var agora = Agora;
        var registro = await sessoes.ObterPorHashAsync(Hash(tokenRenovacao));
        if (registro is null) return null;

        if (registro.RevogadoEm is { } revogadoEm)
        {
            var simultanea = registro.SubstituidoPor is not null
                && agora - revogadoEm < ToleranciaRotacao
                && await sessoes.FamiliaAtivaAsync(registro.Familia, agora);
            if (!simultanea)
            {
                await sessoes.RevogarFamiliaAsync(registro.Familia, agora);
                return null;
            }
        }

        if (registro.ExpiraEm <= agora) return null;

        var usuario = await usuarios.ObterPorIdAsync(registro.UsuarioId);
        if (usuario is null) return null;

        var (novoToken, novoRegistro) = NovoToken(usuario.Id, registro.Familia, registro.Persistente, agora, registro.ExpiraEm);
        registro.SubstituirPor(novoRegistro, agora);
        await sessoes.AdicionarAsync(novoRegistro);
        await unitOfWork.CommitAsync();

        return Emitir(usuario, novoToken, novoRegistro);
    }

    public async Task EncerrarAsync(string? tokenRenovacao)
    {
        if (string.IsNullOrEmpty(tokenRenovacao)) return;
        if (await sessoes.ObterPorHashAsync(Hash(tokenRenovacao)) is { } registro)
            await sessoes.RevogarFamiliaAsync(registro.Familia, Agora);
    }

    /// <summary>Derruba todas as sessões (ex.: depois de trocar a senha).</summary>
    public Task RevogarTodasAsync(int usuarioId) => sessoes.RevogarTodasAsync(usuarioId, Agora);

    /// <summary>A sessão do token de acesso ainda vale? (logout ou troca de senha a encerram na hora).</summary>
    public Task<bool> AtivaAsync(Guid familia) => sessoes.FamiliaAtivaAsync(familia, Agora);

    private SessaoEmitida Emitir(Usuario usuario, string tokenRenovacao, SessaoUsuario registro) =>
        // Com "Lembrar de mim" o cookie é persistente. Sem isso, o cookie some ao fechar o navegador.
        new(usuario, tokensAcesso.Gerar(usuario, registro.Familia), tokenRenovacao, registro.Persistente ? registro.ExpiraEm : null);

    private (string Token, SessaoUsuario Registro) NovoToken(
        int usuarioId, Guid familia, bool persistente, DateTime agora, DateTime? limiteFamilia = null)
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var validade = persistente ? TimeSpan.FromDays(_cfg.DiasLembrarSessao) : TimeSpan.FromHours(_cfg.HorasSessao);
        var expira = agora + validade;
        // A rotação não estende a sessão além do prazo do login original.
        if (limiteFamilia is { } limite && limite < expira) expira = limite;

        return (token, new SessaoUsuario(usuarioId, Hash(token), familia, persistente, agora, expira));
    }

    private static string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
