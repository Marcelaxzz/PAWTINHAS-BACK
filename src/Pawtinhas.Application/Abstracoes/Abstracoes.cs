using Pawtinhas.Application.Email;
using Pawtinhas.Domain;

namespace Pawtinhas.Application.Abstracoes;

/// <summary>Hash de senha (PBKDF2). <see cref="Conferir"/> roda mesmo sem hash, para o tempo de resposta ser igual.</summary>
public interface IHashSenha
{
    string Gerar(string senha);
    bool Conferir(string? hash, string senha);
}

/// <summary>Códigos de 6 dígitos do e-mail; no banco fica só o HMAC.</summary>
public interface IServicoCodigos
{
    string Gerar();
    string Hash(Guid desafioId, string codigo);
    bool Confere(Guid desafioId, string codigoInformado, string hashGuardado);
}

public sealed record TokenAcesso(string Valor, DateTime ExpiraEm);

/// <summary>JWT de acesso, curto, com a família da sessão no claim "sid".</summary>
public interface IGeradorTokenAcesso
{
    TokenAcesso Gerar(Usuario usuario, Guid sessao);
}

/// <summary>Fila em memória: o envio acontece em segundo plano, sem segurar a resposta do login.</summary>
public interface IFilaEmails
{
    void Enfileirar(MensagemEmail mensagem);
}

public interface IEstadoEmail
{
    /// <summary>Há servidor SMTP configurado (ou o modo Pasta, em desenvolvimento).</summary>
    bool Configurado { get; }
}

public sealed record ArquivoFoto(string Caminho, string TipoConteudo);

/// <summary>Fotos enviadas pela plataforma, gravadas com nome aleatório.</summary>
public interface IArmazenamentoFotos
{
    /// <summary>Valida o conteúdo (assinatura de PNG, JPEG ou WebP) e grava. Retorna a URL pública.</summary>
    Task<string> SalvarAsync(Stream conteudo, CancellationToken ct);

    /// <summary>Aceita só URLs geradas por nós ("/api/arquivos/...") ou links https; o resto vira erro de validação.</summary>
    string? ValidarUrl(string? url, string campo);

    void RemoverSeLocal(string? url);

    ArquivoFoto? Localizar(string nome);
}

/// <summary>Cópia completa dos dados em JSON (download manual e backup diário).</summary>
public interface IServicoBackup
{
    Task<byte[]> GerarAsync(CancellationToken ct);
    Task<string> SalvarNaPastaAsync(CancellationToken ct);
    IReadOnlyList<string> Listar();
}
