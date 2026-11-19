using System.Net.Http.Json;
using System.Threading.Channels;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Pawtinhas.Application.Abstracoes;
using Pawtinhas.Application.Email;
using Pawtinhas.Infrastructure.Configuracao;

namespace Pawtinhas.Infrastructure.Email;

public interface IEnviadorEmail
{
    Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct);
}

/// <summary>Envio por SMTP (no Gmail: smtp.gmail.com, porta 587, STARTTLS e senha de app).</summary>
public sealed class EnviadorSmtp(IOptions<EmailOptions> opcoes) : IEnviadorEmail
{
    public async Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct)
    {
        var cfg = opcoes.Value;
        using var cliente = new SmtpClient();
        await cliente.ConnectAsync(cfg.Host, cfg.Porta, Seguranca(cfg.Seguranca), ct);
        if (!string.IsNullOrEmpty(cfg.Usuario)) await cliente.AuthenticateAsync(cfg.Usuario, cfg.Senha, ct);
        await cliente.SendAsync(Montar(cfg, mensagem), ct);
        await cliente.DisconnectAsync(true, ct);
    }

    internal static MimeMessage Montar(EmailOptions cfg, MensagemEmail m)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(cfg.NomeRemetente, cfg.Remetente));
        mime.To.Add(new MailboxAddress(m.NomePara, m.Para));
        mime.Subject = m.Assunto;
        mime.Body = new BodyBuilder { HtmlBody = m.Html, TextBody = m.Texto }.ToMessageBody();
        return mime;
    }

    private static SecureSocketOptions Seguranca(string valor) => valor switch
    {
        "StartTls" => SecureSocketOptions.StartTls,
        "SslOnConnect" => SecureSocketOptions.SslOnConnect,
        "Nenhuma" => SecureSocketOptions.None,
        _ => SecureSocketOptions.Auto,
    };
}

/// <summary>
/// Envio por HTTPS: a função api/correio do site (Vercel) confere o token e entrega o e-mail ao Gmail
/// da clínica. O e-mail sai da própria conta, como no SMTP.
/// </summary>
public sealed class EnviadorHttp(IOptions<EmailOptions> opcoes) : IEnviadorEmail
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public async Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct)
    {
        var cfg = opcoes.Value;
        using var resposta = await Http.PostAsJsonAsync(cfg.UrlEnvio, new
        {
            token = cfg.TokenEnvio,
            para = mensagem.Para,
            assunto = mensagem.Assunto,
            html = mensagem.Html,
            texto = mensagem.Texto,
            nome = cfg.NomeRemetente,
        }, ct);

        var corpo = (await resposta.Content.ReadAsStringAsync(ct)).Trim();
        if (!resposta.IsSuccessStatusCode || corpo != "ok")
        {
            // Só o começo da resposta vai para o log: nunca o conteúdo do e-mail.
            throw new InvalidOperationException(
                $"O serviço de envio recusou o e-mail (HTTP {(int)resposta.StatusCode}): {corpo[..Math.Min(corpo.Length, 120)]}");
        }
    }
}

/// <summary>Somente desenvolvimento: grava cada e-mail como .eml numa pasta, sem enviar.</summary>
public sealed class EnviadorPasta(IOptions<EmailOptions> opcoes, IHostEnvironment env, ILogger<EnviadorPasta> log) : IEnviadorEmail
{
    public async Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct)
    {
        var pasta = Path.GetFullPath(Path.Combine(env.ContentRootPath, opcoes.Value.Pasta));
        Directory.CreateDirectory(pasta);
        var arquivo = Path.Combine(pasta, $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.eml");
        await EnviadorSmtp.Montar(opcoes.Value, mensagem).WriteToAsync(arquivo, ct);
        log.LogInformation("E-mail \"{Assunto}\" gravado em {Arquivo}", mensagem.Assunto, arquivo);
    }
}

/// <summary>O e-mail está configurado quando o modo escolhido tem o que precisa para enviar.</summary>
public sealed class EstadoEmail(IOptions<EmailOptions> opcoes) : IEstadoEmail
{
    public bool Configurado => opcoes.Value.Modo switch
    {
        ModoEmail.Pasta => true,
        ModoEmail.Http => !string.IsNullOrEmpty(opcoes.Value.UrlEnvio) && !string.IsNullOrEmpty(opcoes.Value.TokenEnvio),
        _ => !string.IsNullOrWhiteSpace(opcoes.Value.Host) && !string.IsNullOrEmpty(opcoes.Value.Senha),
    };
}

/// <summary>Fila em memória lida pelo <see cref="ServicoEnvioEmails"/>.</summary>
public sealed class FilaEmails : IFilaEmails
{
    private readonly Channel<MensagemEmail> _canal = Channel.CreateBounded<MensagemEmail>(
        new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest });

    public void Enfileirar(MensagemEmail mensagem) => _canal.Writer.TryWrite(mensagem);

    public IAsyncEnumerable<MensagemEmail> LerTodasAsync(CancellationToken ct) => _canal.Reader.ReadAllAsync(ct);
}

/// <summary>Envia os e-mails da fila, com até três novas tentativas em caso de falha.</summary>
public sealed class ServicoEnvioEmails(FilaEmails fila, IEnviadorEmail enviador, ILogger<ServicoEnvioEmails> log) : BackgroundService
{
    private static readonly TimeSpan[] Esperas = [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var mensagem in fila.LerTodasAsync(stoppingToken))
        {
            for (var tentativa = 0; ; tentativa++)
            {
                try
                {
                    await enviador.EnviarAsync(mensagem, stoppingToken);
                    // O assunto não vai para o log: o do login traz o código de acesso.
                    log.LogInformation("E-mail enviado.");
                    break;
                }
                catch (Exception ex) when (ex is not OperationCanceledException && tentativa < Esperas.Length)
                {
                    log.LogWarning(ex, "Falha ao enviar e-mail (tentativa {Tentativa}). Tentando de novo.", tentativa + 1);
                    await Task.Delay(Esperas[tentativa], stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.LogError(ex, "Não foi possível enviar o e-mail. Confira a seção Email da configuração.");
                    break;
                }
            }
        }
    }
}
