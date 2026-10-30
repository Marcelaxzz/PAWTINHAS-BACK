using System.ComponentModel.DataAnnotations;
using Pawtinhas.Domain.Enums;

namespace Pawtinhas.Application.DTOs;

public class CadastrarAnimalDto
{
    [Range(1, int.MaxValue)]
    public int ResponsavelId { get; set; }

    [Required]
    public string Nome { get; set; } = string.Empty;

    [Required]
    public string Sexo { get; set; } = string.Empty;

    public string? Especie { get; set; }
    public string? Raca { get; set; }
    public DateTime? DataNascimento { get; set; }
    public PorteAnimal? Porte { get; set; }
    public decimal? PesoKg { get; set; }
    public string? Observacoes { get; set; }
}