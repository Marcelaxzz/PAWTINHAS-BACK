using System.Net.Mail;
using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>Pessoa da equipe (não é um login). A função define em quais agendamentos ela aparece.</summary>
public class Profissional
{
    public static readonly string[] Funcoes = ["Veterinária", "Veterinário", "Técnico", "Groomer", "Pet Sitter", "Recepcionista"];

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Funcao { get; private set; } = string.Empty;
    public string? Email { get; private set; }

    private Profissional() { }

    public Profissional(string nome, string funcao, string? email) => Atualizar(nome, funcao, email);

    public void Atualizar(string nome, string funcao, string? email)
    {
        Nome = Validar.Texto(nome, "nome", 80, "Informe o nome.");
        Funcao = Validar.Opcao(funcao, Funcoes, "funcao", "Escolha a função.");
        var texto = Validar.TextoOpcional(email, "email", 150)?.ToLowerInvariant();
        if (texto is not null && !(MailAddress.TryCreate(texto, out var endereco) && endereco.Address == texto))
            throw new DominioException("email", "Informe um e-mail válido.");
        Email = texto;
    }

    /// <summary>"Veterinário", "Recepcionista" ou "Equipe" (agrupamento da tela de equipe).</summary>
    public string Papel => Funcao is "Veterinária" or "Veterinário" ? "Veterinário" : Funcao == "Recepcionista" ? Funcao : "Equipe";

    public string Iniciais
    {
        get
        {
            var partes = Nome.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return partes.Length == 0 ? "?" : (partes.Length == 1
                ? partes[0][..1]
                : $"{partes[0][0]}{partes[^1][0]}").ToUpperInvariant();
        }
    }
}
