
namespace Pawtinhas.Domain;

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
        NomeCompleto = nomeCompleto;
        Telefone = telefone;
        CPF = cpf;
        Email = email;
        CEP = cep;
        Endereco = endereco;
        Cidade = cidade;
        UF = uf;
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
        NomeCompleto = nomeCompleto;
        Telefone = telefone;
        CPF = cpf;
        Email = email;
        CEP = cep;
        Endereco = endereco;
        Cidade = cidade;
        UF = uf;
    }
}
