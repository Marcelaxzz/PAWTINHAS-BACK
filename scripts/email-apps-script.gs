// Pawtinhas: envio dos e-mails (códigos de acesso, avisos) pelo Gmail da clínica.
// Publicado como "App da Web" na conta pawtinhas04@gmail.com. A API manda cada e-mail
// para este endereço por HTTPS e o script envia pelo Gmail.

const TOKEN = 'COLE_AQUI_O_MESMO_VALOR_DE_Email__TokenAppsScript';

function doPost(e) {
  try {
    const d = JSON.parse(e.postData.contents);
    if (d.token !== TOKEN) return responder('negado');
    GmailApp.sendEmail(d.para, d.assunto, d.texto || '', { htmlBody: d.html, name: d.nome || 'Pawtinhas' });
    return responder('ok');
  } catch (erro) {
    return responder('erro: ' + erro.message);
  }
}

function responder(texto) {
  return ContentService.createTextOutput(texto).setMimeType(ContentService.MimeType.TEXT);
}

// Rode esta função uma vez (botão Executar) para o Google pedir a permissão do Gmail.
function testar() {
  GmailApp.sendEmail(Session.getActiveUser().getEmail(), 'Pawtinhas: teste de envio', 'O envio de e-mails está funcionando.');
}