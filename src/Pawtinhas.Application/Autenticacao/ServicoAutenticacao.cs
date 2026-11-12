using Microsoft.Extensions.Options;
using Pawtinhas.Application.Abstracoes;
using Pawtinhas.Application.Comum;
using Pawtinhas.Application.Configuracao;
using Pawtinhas.Application.Email;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Comum;
using Pawtinhas.Domain.Interfaces;
using Pawtinhas.Domain.Seguranca;

namespace Pawtinhas.Application.Autenticacao;

/// <summary>
/// Login em duas etapas: e-mail e senha, depois o código de 6 dígitos enviado por e-mail.
/// Só o e-mail de <see cref="AutenticacaoOptions.EmailPermitido"/> entra. As respostas são as mesmas
/// para qualquer e-mail, então nenhuma rota revela qual é o endereço aceito.
/// </summary>
public sealed class ServicoAutenticacao(
    IUsuarioRepository usuarios,
    IDesafioCodigoRepository desafios,
    IUnitOfWork unitOfWork,
    IHashSenha senhas,
    IServicoCodigos codigos,
    ServicoSessoes sessoes,
    IFilaEmails emails,
    ModelosEmail modelos,
    IOptions<AutenticacaoOptions> opcoes,
    TimeProvider relogio)
{
    /// <summary>Pedidos de troca de senha que geram código por hora. Passado o teto, nenhum e-mail sai.</summary>
    private const int MaximoRedefinicoesPorHora = 3;

    private readonly AutenticacaoOptions _cfg = opcoes.Value;

    private DateTime Agora => relogio.GetUtcNow().UtcDateTime;
    private TimeSpan Validade => TimeSpan.FromMinutes(_cfg.MinutosValidadeCodigo);
    private TimeSpan EsperaReenvio => TimeSpan.FromSeconds(_cfg.SegundosEntreReenvios);

    /// <summary>Confere e-mail e senha e envia o código de acesso.</summary>
    public async Task<DesafioResposta> EntrarAsync(LoginRequisicao req)
    {
        var agora = Agora;
        var usuario = await BuscarPermitidoAsync(req.Identifier);

        // A senha é conferida sempre (mesmo sem conta) para o tempo de resposta não denunciar nada.
        var senhaConfere = senhas.Conferir(usuario?.SenhaHash, req.Password);

        if (usuario is not null && usuario.Bloqueado(agora))
            throw MuitasTentativas(usuario.BloqueadoAte!.Value - agora);

        if (usuario is null || !senhaConfere)
        {
            if (usuario is not null)
            {
                usuario.RegistrarFalha(agora, _cfg.FalhasAteBloqueio, TimeSpan.FromMinutes(_cfg.MinutosBloqueio));
                await unitOfWork.CommitAsync();
            }
            throw new AutenticacaoException(401, "credenciais_invalidas", "E-mail ou senha incorretos.");
        }

        usuario.RegistrarSucesso();
        var (desafio, codigo) = NovoDesafio(FinalidadeCodigo.Login, usuario.Id, req.Remember, null, agora);
        await desafios.AdicionarAsync(desafio);
        await unitOfWork.CommitAsync();

        emails.Enfileirar(modelos.CodigoLogin(usuario.Email, usuario.Nome, codigo));
        return new DesafioResposta(desafio.Id, _cfg.SegundosEntreReenvios);
    }

    /// <summary>Confere o código do e-mail e abre a sessão.</summary>
    public async Task<SessaoEmitida> ConfirmarLoginAsync(CodigoRequisicao req)
    {
        var desafio = await ConferirCodigoAsync(req, FinalidadeCodigo.Login);
        var usuario = await usuarios.ObterPorIdAsync(desafio.UsuarioId ?? 0) ?? throw AutenticacaoException.CodigoExpirado();
        return await sessoes.IniciarAsync(usuario, desafio.LembrarSessao);
    }

    public async Task<ReenvioResposta> ReenviarCodigoAsync(ReenvioRequisicao req)
    {
        var agora = Agora;
        var desafio = await desafios.ObterAsync(req.ChallengeId);
        if (desafio is null || !desafio.Valido(agora)) throw AutenticacaoException.CodigoExpirado();

        if (desafio.ReenvioPermitidoEm > agora)
            throw new AutenticacaoException(429, "reenvio_cedo", "Aguarde para reenviar o código.", desafio.ReenvioPermitidoEm - agora);
        if (desafio.Reenvios >= _cfg.MaximoReenvios)
            throw new AutenticacaoException(429, "reenvio_limite", "Limite de reenvios atingido. Volte e comece de novo.");

        var codigo = codigos.Gerar();
        desafio.Reenviar(codigos.Hash(desafio.Id, codigo), agora, Validade, EsperaReenvio);
        await unitOfWork.CommitAsync();

        if (desafio.UsuarioId is { } id && await usuarios.ObterPorIdAsync(id) is { } usuario)
        {
            emails.Enfileirar(desafio.Finalidade == FinalidadeCodigo.Login
                ? modelos.CodigoLogin(usuario.Email, usuario.Nome, codigo)
                : modelos.CodigoRedefinicao(usuario.Email, usuario.Nome, codigo));
        }

        return new ReenvioResposta(_cfg.SegundosEntreReenvios);
    }

    /// <summary>Pede a troca de senha. A nova senha só vale depois que o código do e-mail for confirmado.</summary>
    public async Task<DesafioResposta> SolicitarRedefinicaoAsync(RedefinicaoRequisicao req)
    {
        if (PoliticaSenha.ProblemaDeForca(req.Password) is { } problema) throw new DominioException("password", problema);

        var agora = Agora;
        var usuario = await BuscarPermitidoAsync(req.Email);
        var hashNovaSenha = senhas.Gerar(req.Password);

        // Cada pedido abre um código novo com 5 tentativas; sem teto, pedir várias vezes daria mais
        // chances de acertar. Passado o limite, a resposta é a mesma, mas nada é enviado.
        if (usuario is not null
            && await desafios.ContarDesdeAsync(usuario.Id, FinalidadeCodigo.RedefinicaoSenha, agora.AddHours(-1)) >= MaximoRedefinicoesPorHora)
        {
            usuario = null;
        }

        var (desafio, codigo) = NovoDesafio(FinalidadeCodigo.RedefinicaoSenha, usuario?.Id, false, hashNovaSenha, agora);
        await desafios.AdicionarAsync(desafio);
        await unitOfWork.CommitAsync();

        if (usuario is not null) emails.Enfileirar(modelos.CodigoRedefinicao(usuario.Email, usuario.Nome, codigo));
        return new DesafioResposta(desafio.Id, _cfg.SegundosEntreReenvios);
    }

    /// <summary>Confere o código, troca a senha e encerra todas as sessões abertas.</summary>
    public async Task ConfirmarRedefinicaoAsync(CodigoRequisicao req)
    {
        var desafio = await ConferirCodigoAsync(req, FinalidadeCodigo.RedefinicaoSenha);
        var usuario = await usuarios.ObterPorIdAsync(desafio.UsuarioId ?? 0);
        if (usuario is null || desafio.SenhaPendenteHash is null) throw AutenticacaoException.CodigoExpirado();

        usuario.DefinirSenha(desafio.SenhaPendenteHash, Agora);
        await unitOfWork.CommitAsync();
        await sessoes.RevogarTodasAsync(usuario.Id);

        emails.Enfileirar(modelos.SenhaAlterada(usuario.Email, usuario.Nome));
    }

    public async Task<UsuarioResposta> ObterUsuarioAsync(int id) =>
        await usuarios.ObterPorIdAsync(id) is { } usuario ? UsuarioResposta.De(usuario) : throw AutenticacaoException.SessaoExpirada();

    // ---------- Auxiliares ----------

    /// <summary>A conta só é buscada se o e-mail for o permitido; qualquer outro se comporta como "sem conta".</summary>
    private async Task<Usuario?> BuscarPermitidoAsync(string email)
    {
        var normalizado = Usuario.NormalizarEmail(email);
        return normalizado == Usuario.NormalizarEmail(_cfg.EmailPermitido)
            ? await usuarios.ObterPorEmailAsync(normalizado)
            : null;
    }

    private (DesafioCodigo Desafio, string Codigo) NovoDesafio(
        FinalidadeCodigo finalidade, int? usuarioId, bool lembrar, string? senhaPendente, DateTime agora)
    {
        var codigo = codigos.Gerar();
        var desafio = new DesafioCodigo(finalidade, usuarioId, lembrar, senhaPendente, agora, Validade, EsperaReenvio);
        desafio.DefinirCodigo(codigos.Hash(desafio.Id, codigo));
        return (desafio, codigo);
    }

    private async Task<DesafioCodigo> ConferirCodigoAsync(CodigoRequisicao req, FinalidadeCodigo finalidade)
    {
        var agora = Agora;
        var desafio = await desafios.ObterAsync(req.ChallengeId, finalidade);
        if (desafio is null || !desafio.Valido(agora)) throw AutenticacaoException.CodigoExpirado();

        if (desafio.Tentativas >= _cfg.TentativasPorCodigo)
        {
            desafios.Remover(desafio);
            await unitOfWork.CommitAsync();
            throw new AutenticacaoException(429, "codigo_bloqueado", "Muitas tentativas incorretas. Solicite um novo código.");
        }

        if (!codigos.Confere(desafio.Id, req.Code, desafio.CodigoHash))
        {
            desafio.RegistrarTentativaErrada();
            await unitOfWork.CommitAsync();
            var restantes = _cfg.TentativasPorCodigo - desafio.Tentativas;
            throw new DominioException("code", restantes > 0
                ? $"Código incorreto. Você ainda tem {restantes} {(restantes == 1 ? "tentativa" : "tentativas")}."
                : "Muitas tentativas incorretas. Solicite um novo código.");
        }

        desafio.MarcarUsado(agora);
        await unitOfWork.CommitAsync();
        return desafio;
    }

    private static AutenticacaoException MuitasTentativas(TimeSpan espera) =>
        new(429, "login_bloqueado", "Muitas tentativas. Aguarde alguns minutos e tente novamente.", espera);
}
