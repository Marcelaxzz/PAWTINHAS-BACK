using System.ComponentModel.DataAnnotations;
using Pawtinhas.Domain.Enums;

namespace Pawtinhas.Application.DTOs;

/// <summary>Cadastro de paciente: todo paciente tem um responsável (tutor).</summary>
public class CadastrarAnimalDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecione o tutor responsável.")]
    public int ResponsavelId { get; set; }

    [Required(ErrorMessage = "Informe o nome do animal.")]
    [StringLength(60)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escolha o sexo.")]
    public string Sexo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a espécie.")]
    [StringLength(40)]
    public string? Especie { get; set; }

    [StringLength(60)]
    public string? Raca { get; set; }

    public DateTime? DataNascimento { get; set; }
    public PorteAnimal? Porte { get; set; }
    public decimal? PesoKg { get; set; }

    [StringLength(1000)]
    public string? Observacoes { get; set; }

    [StringLength(500)]
    public string? FotoUrl { get; set; }

    [StringLength(60)]
    public string? Localizacao { get; set; }

    public PersonalidadeAnimal? Personalidade { get; set; }
}
