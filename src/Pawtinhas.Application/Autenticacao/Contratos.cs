using System.ComponentModel.DataAnnotations;
using Pawtinhas.Application.Abstracoes;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Seguranca;
using Pawtinhas.Domain.Enums;

namespace Pawtinhas.Application.Autenticacao;

public sealed record LoginRequisicao(
    [Required(ErrorMessage = "Informe seu e-mail."), StringLength(254)] string Identifier,
    [Required(ErrorMessage = "Informe sua senha."), StringLength(PoliticaSenha.Maximo)] string Password,
    bool Remember);

public sealed record CodigoRequisicao(
    Guid ChallengeId,
    [Required(ErrorMessage = "Digite os 6 dígitos do código."), RegularExpression(@"^\d{6}$", ErrorMessage = "Digite os 6 dígitos do código.")] string Code);

public sealed record ReenvioRequisicao(Guid ChallengeId);

public sealed record RedefinicaoRequisicao(
    [Required(ErrorMessage = "Informe seu e-mail."), EmailAddress(ErrorMessage = "Informe um e-mail válido."), StringLength(254)] string Email,
    [Required(ErrorMessage = "Crie uma senha."), StringLength(PoliticaSenha.Maximo)] string Password);

public sealed record DesafioResposta(Guid ChallengeId, int ResendIn);

public sealed record ReenvioResposta(int ResendIn);

public sealed record UsuarioResposta(string Id, string Name, string Email, string Role)
{
    public static UsuarioResposta De(Usuario u) => new(u.Id.ToString(), u.Nome, u.Email, Papeis.De(u.Papel));
}

/// <summary>User nulo = visitante sem sessão.</summary>
public sealed record SessaoResposta(UsuarioResposta? User);

/// <summary>Sessão recém-aberta ou renovada: os tokens que a API grava nos cookies.</summary>
/// <param name="ExpiraRenovacao">Nulo = cookie de sessão (some ao fechar o navegador). Preenchido com "Lembrar de mim".</param>
public sealed record SessaoEmitida(Usuario Usuario, TokenAcesso Acesso, string TokenRenovacao, DateTime? ExpiraRenovacao);

public static class Papeis
{
    /// <summary>Administradora da plataforma: a veterinária responsável pela clínica.</summary>
    public const string Veterinaria = "veterinaria";

    /// <summary>Tutor logado: só enxerga os próprios animais e compromissos.</summary>
    public const string Tutor = "tutor";

    public static string De(PapelUsuario papel) => papel switch
    {
        PapelUsuario.VETERINARIA => Veterinaria,
        PapelUsuario.TUTOR => Tutor,
        _ => throw new InvalidOperationException("Papel de usuário desconhecido."),
    };
}

public static class Politicas
{
    public const string Admin = "Admin";
    public const string Tutor = "Tutor";
}

/// <summary>Nomes dos claims do JWT de acesso.</summary>
public static class Claims
{
    public const string Sessao = "sid";

    /// <summary>Id do tutor (Responsavel) ligado à conta. Só existe em tokens de tutor.</summary>
    public const string Responsavel = "rid";
}

public sealed record CriarContaTutorRequisicao(
    [Range(1, int.MaxValue, ErrorMessage = "Informe o tutor.")] int ResponsavelId,
    [Required(ErrorMessage = "Informe o e-mail."), EmailAddress(ErrorMessage = "Informe um e-mail válido."), StringLength(254)] string Email);

public sealed record ContaTutorResposta(int Id, int ResponsavelId, string Nome, string Email, DateTime CriadoEm)
{
    public static ContaTutorResposta De(Usuario u) => new(u.Id, u.ResponsavelId ?? 0, u.Nome, u.Email, u.CriadoEm);
}