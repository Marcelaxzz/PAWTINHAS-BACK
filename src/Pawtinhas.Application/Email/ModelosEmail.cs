using System.Net;
using Microsoft.Extensions.Options;
using Pawtinhas.Application.Configuracao;

namespace Pawtinhas.Application.Email;

public sealed record MensagemEmail(string Para, string NomePara, string Assunto, string Html, string Texto);

/// <summary>Um compromisso do dia, para o resumo enviado por e-mail.</summary>
public sealed record ItemAgenda(TimeOnly Hora, string Tipo, string Pet, string Detalhe);

/// <summary>Textos dos e-mails da plataforma (HTML com estilos inline + versão em texto puro).</summary>
public sealed class ModelosEmail(IOptions<AutenticacaoOptions> auth, IOptions<FrontendOptions> frontend)
{
    private int Minutos => auth.Value.MinutosValidadeCodigo;

    public MensagemEmail CodigoLogin(string email, string nome, string codigo) => Codigo(
        email, nome, codigo,
        assunto: $"{codigo} é o seu código de acesso ao Pawtinhas",
        introducao: "Use o código abaixo para concluir a entrada no painel da clínica.");

    public MensagemEmail CodigoRedefinicao(string email, string nome, string codigo) => Codigo(
        email, nome, codigo,
        assunto: $"{codigo} é o seu código para redefinir a senha",
        introducao: "Recebemos um pedido para trocar a senha da sua conta. Use o código abaixo para confirmar.");

    public MensagemEmail SenhaAlterada(string email, string nome)
    {
        const string titulo = "Sua senha foi alterada";
        const string corpo = "A senha da sua conta no Pawtinhas acabou de ser alterada e todas as sessões abertas foram encerradas. "
            + "Se não foi você, redefina a senha imediatamente pela opção “Esqueceu a senha?”.";
        return new MensagemEmail(email, nome, titulo,
            Layout(titulo, $"<p style=\"margin:0 0 16px;\">Olá, {Html(PrimeiroNome(nome))}!</p><p style=\"margin:0;\">{Html(corpo)}</p>"),
            $"Olá, {PrimeiroNome(nome)}!\n\n{corpo}\n\nEquipe Pawtinhas");
    }

    public MensagemEmail ResumoDiario(string email, string nome, DateOnly dia, IReadOnlyList<ItemAgenda> itens)
    {
        var data = dia.ToString("dd/MM/yyyy");
        const string titulo = "Agenda de hoje";
        var assunto = itens.Count == 0
            ? $"Agenda de {data}: nenhum compromisso"
            : $"Agenda de {data}: {itens.Count} {(itens.Count == 1 ? "compromisso" : "compromissos")}";

        var linhas = itens.Count == 0
            ? "<p style=\"margin:0;\">Nenhum compromisso agendado para hoje.</p>"
            : "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:collapse;\">"
              + string.Concat(itens.Select(i => $"""
                  <tr>
                    <td style="padding:10px 12px 10px 0;border-bottom:1px solid #f0e4d4;font-weight:700;white-space:nowrap;vertical-align:top;">{i.Hora:HH\:mm}</td>
                    <td style="padding:10px 0;border-bottom:1px solid #f0e4d4;">
                      <strong>{Html(i.Pet)}</strong> · {Html(i.Tipo)}<br>
                      <span style="color:#897260;font-size:13px;">{Html(i.Detalhe)}</span>
                    </td>
                  </tr>
                  """))
              + "</table>";

        var html = Layout(titulo, $"<p style=\"margin:0 0 16px;\">Bom dia, {Html(PrimeiroNome(nome))}! Estes são os compromissos de {data}:</p>{linhas}");
        var texto = $"Bom dia, {PrimeiroNome(nome)}! Compromissos de {data}:\n\n"
            + (itens.Count == 0
                ? "Nenhum compromisso agendado para hoje."
                : string.Join("\n", itens.Select(i => $"{i.Hora:HH\\:mm}  {i.Pet} · {i.Tipo}: {i.Detalhe}")))
            + "\n\nEquipe Pawtinhas";
        return new MensagemEmail(email, nome, assunto, html, texto);
    }

    private MensagemEmail Codigo(string email, string nome, string codigo, string assunto, string introducao)
    {
        var aviso = $"O código vale por {Minutos} minutos e só pode ser usado uma vez. "
            + "Se você não fez esse pedido, ignore este e-mail. Ninguém consegue entrar sem a sua senha.";

        var html = Layout("Verificação de segurança", $"""
            <p style="margin:0 0 16px;">Olá, {Html(PrimeiroNome(nome))}!</p>
            <p style="margin:0 0 24px;">{Html(introducao)}</p>
            <p style="margin:0 0 24px;text-align:center;">
              <span style="display:inline-block;padding:16px 28px;border-radius:16px;background:#fbf6eb;border:2px solid #d8a16b;
                font-family:'Courier New',monospace;font-size:34px;font-weight:700;letter-spacing:10px;color:#36241a;">{codigo}</span>
            </p>
            <p style="margin:0;font-size:13px;color:#897260;">{Html(aviso)}</p>
            """);

        var texto = $"Olá, {PrimeiroNome(nome)}!\n\n{introducao}\n\nCódigo: {codigo}\n\n{aviso}\n\nEquipe Pawtinhas";
        return new MensagemEmail(email, nome, assunto, html, texto);
    }

    private string Layout(string titulo, string conteudo)
    {
        var site = frontend.Value.Url;
        var rodape = Uri.TryCreate(site, UriKind.Absolute, out var uri)
            ? $"Pawtinhas · <a href=\"{Html(site)}\" style=\"color:#6e4d36;\">{Html(uri.Host)}</a>"
            : "Pawtinhas · gestão veterinária e adoção responsável";

        return $"""
            <!doctype html>
            <html lang="pt-BR">
            <body style="margin:0;padding:32px 16px;background:#f4e6d3;font-family:Arial,Helvetica,sans-serif;color:#36241a;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                <tr><td align="center">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;background:#ffffff;border-radius:24px;overflow:hidden;">
                    <tr><td style="background:#6e4d36;padding:22px 32px;color:#ffffff;font-size:20px;font-weight:700;letter-spacing:2px;">PAWTINHAS</td></tr>
                    <tr><td style="padding:32px;font-size:15px;line-height:1.6;">
                      <h1 style="margin:0 0 20px;font-family:Georgia,serif;font-size:24px;color:#36241a;">{Html(titulo)}</h1>
                      {conteudo}
                    </td></tr>
                    <tr><td style="padding:18px 32px;background:#fbf6eb;font-size:12px;color:#897260;">{rodape}</td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string PrimeiroNome(string nome) => nome.Trim().Split(' ', 2)[0];

    private static string Html(string texto) => WebUtility.HtmlEncode(texto);
}
