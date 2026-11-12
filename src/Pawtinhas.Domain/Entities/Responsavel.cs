
using System.Net.Mail;
using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>Tutor (responsável) pelos animais atendidos.</summary>
public class Responsavel
{
    public int Id { get; private set; }
    public string NomeCompleto { get; private set; } = string.Empty;
    public string? CPF { get; private set; }
    public string Telefone { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? CEP { get; private set; }
    public string? Endereco { get; private set; }
    public string? Cidade { get; private set; }
    public string? UF { get; private set; }
    public DateTime CriadoEm { get; private set; } = DateTime.UtcNow;
    public ICollection<Animal> Animais { get; private set; } = new List<Animal>();
    private Responsavel() { }
    public Responsavel(
        string nomeCompleto,
        string telefone,
        string? cpf = null,
        string? email = null,
        string? cep = null,
        string? endereco = null,
        string? cidade = null,
        string? uf = null)
    {
        AtualizarDados(nomeCompleto, telefone, cpf, email, cep, endereco, cidade, uf);
    }
    public void AtualizarDados(
        string nomeCompleto,
        string telefone,
        string? cpf,
        string? email,
        string? cep,
        string? endereco,
        string? cidade,
        string? uf)
    {
        NomeCompleto = Validar.Texto(nomeCompleto, "nomeCompleto", 150, "Informe o nome do tutor.");
        Telefone = NormalizarTelefone(telefone);
        CPF = NormalizarCpf(cpf);
        Email = NormalizarEmail(email);
        CEP = NormalizarCep(cep);
        Endereco = Validar.TextoOpcional(endereco, "endereco", 200);
        Cidade = Validar.TextoOpcional(cidade, "cidade", 100);
        UF = NormalizarUf(uf);
    }

    private static string NormalizarTelefone(string? telefone)
    {
        var digitos = new string((telefone ?? "").Where(char.IsDigit).ToArray());
        if (digitos.Length is < 10 or > 11)
            throw new DominioException("telefone", "Informe o telefone com DDD (10 ou 11 dígitos).");
        return digitos.Length == 11
            ? $"({digitos[..2]}) {digitos[2..7]}-{digitos[7..]}"
            : $"({digitos[..2]}) {digitos[2..6]}-{digitos[6..]}";
    }

    /// <summary>CPF opcional; quando informado, os dígitos verificadores precisam conferir.</summary>
    private static string? NormalizarCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return null;
        var d = new string(cpf.Where(char.IsDigit).ToArray());
        if (d.Length != 11 || d.Distinct().Count() == 1 || !DigitoConfere(d, 9) || !DigitoConfere(d, 10))
            throw new DominioException("cpf", "CPF inválido. Confira os números.");
        return $"{d[..3]}.{d[3..6]}.{d[6..9]}-{d[9..]}";

        static bool DigitoConfere(string d, int posicao)
        {
            var soma = 0;
            for (var i = 0; i < posicao; i++) soma += (d[i] - '0') * (posicao + 1 - i);
            var resto = soma * 10 % 11 % 10;
            return resto == d[posicao] - '0';
        }
    }

    private static string? NormalizarEmail(string? email)
    {
        var texto = Validar.TextoOpcional(email, "email", 150)?.ToLowerInvariant();
        if (texto is null) return null;
        return MailAddress.TryCreate(texto, out var endereco) && endereco.Address == texto
            ? texto
            : throw new DominioException("email", "Informe um e-mail válido.");
    }

    private static string? NormalizarCep(string? cep)
    {
        if (string.IsNullOrWhiteSpace(cep)) return null;
        var d = new string(cep.Where(char.IsDigit).ToArray());
        return d.Length == 8 ? $"{d[..5]}-{d[5..]}" : throw new DominioException("cep", "O CEP tem 8 dígitos.");
    }

    private static string? NormalizarUf(string? uf)
    {
        var texto = uf?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(texto)) return null;
        return texto.Length == 2 && texto.All(char.IsAsciiLetterUpper)
            ? texto
            : throw new DominioException("uf", "Use a sigla do estado com 2 letras (ex.: MG).");
    }
}
