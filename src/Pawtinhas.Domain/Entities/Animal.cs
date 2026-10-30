
using Pawtinhas.Domain.Enums;

namespace Pawtinhas.Domain;

public class Animal
{
    public int Id { get; private set; }

    public int ResponsavelId { get; private set; }
    public Responsavel Responsavel { get; private set; } = null!;

    public SituacaoAnimal Status { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Sexo { get; private set; } = string.Empty;
    public string? Especie { get; private set; }
    public string? Raca { get; private set; }
    public DateTime? DataNascimento { get; private set; }
    public PorteAnimal? Porte { get; private set; }
    public decimal? PesoKg { get; private set; }
    public string? Observacoes { get; private set; }
    public DateTime CriadoEm { get; private set; } = DateTime.UtcNow;

    public string? FotoUrl { get; private set; }
    public string? Localizacao { get; private set; }
    public PersonalidadeAnimal? Personalidade { get; private set; }

    private Animal() { }

    public Animal(
        int responsavelId,
        string nome,
        string especie,
        string sexo,
        string? raca = null,
        DateTime? dataNascimento = null,
        PorteAnimal? porte = null,
        decimal? pesoKg = null,
        string? observacoes = null,
        string? fotoUrl = null,
        string? localizacao = null,
        PersonalidadeAnimal? personalidade = null)
    {
        ResponsavelId = responsavelId;
        Nome = nome;
        Especie = especie;
        Sexo = sexo;
        Raca = raca;
        DataNascimento = dataNascimento;
        Porte = porte;
        PesoKg = pesoKg;
        Observacoes = observacoes;
        Status = SituacaoAnimal.ATIVO;
        FotoUrl = fotoUrl;
        Localizacao = localizacao;
        Personalidade = personalidade;
    }

    public void AtualizarDados(
        int responsavelId,
        string nome,
        string especie,
        string sexo,
        string? raca,
        DateTime? dataNascimento,
        PorteAnimal? porte,
        decimal? pesoKg,
        string? observacoes,
        string? fotoUrl,
        string? localizacao,
        PersonalidadeAnimal? personalidade)
    {
        ResponsavelId = responsavelId;
        Nome = nome;
        Especie = especie;
        Sexo = sexo;
        Raca = raca;
        DataNascimento = dataNascimento;
        Porte = porte;
        PesoKg = pesoKg;
        Observacoes = observacoes;
        FotoUrl = fotoUrl;
        Localizacao = localizacao;
        Personalidade = personalidade;
    }
}
