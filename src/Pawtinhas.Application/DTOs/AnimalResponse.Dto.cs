
using Pawtinhas.Domain;
using Pawtinhas.Domain.Enums;

namespace Pawtinhas.Application.DTOs;

public class AnimalResponseDto
{
    public int Id { get; set; }
    public int ResponsavelId { get; set; }
    public SituacaoAnimal Status { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public string? Especie { get; set; }
    public string? Raca { get; set; }
    public DateTime? DataNascimento { get; set; }
    public PorteAnimal? Porte { get; set; }
    public decimal? PesoKg { get; set; }
    public string? Observacoes { get; set; }
    public DateTime CriadoEm { get; set; }

    public string? FotoUrl { get; set; }
    public string? Localizacao { get; set; }
    public PersonalidadeAnimal? Personalidade { get; set; }

    public static AnimalResponseDto FromEntity(Animal animal)
    {
        return new AnimalResponseDto
        {
            Id = animal.Id,
            ResponsavelId = animal.ResponsavelId,
            Status = animal.Status,
            Nome = animal.Nome,
            Sexo = animal.Sexo,
            Especie = animal.Especie,
            Raca = animal.Raca,
            DataNascimento = animal.DataNascimento,
            Porte = animal.Porte,
            PesoKg = animal.PesoKg,
            Observacoes = animal.Observacoes,
            CriadoEm = animal.CriadoEm,
            FotoUrl = animal.FotoUrl,
            Localizacao = animal.Localizacao,
            Personalidade = animal.Personalidade
        };
    }
}
