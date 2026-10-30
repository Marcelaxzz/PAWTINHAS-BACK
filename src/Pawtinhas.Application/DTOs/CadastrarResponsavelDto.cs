
using System.ComponentModel.DataAnnotations;

namespace Pawtinhas.Application.DTOs;

public class CadastrarResponsavelDto
{
    [Required]
    [StringLength(150)]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Telefone { get; set; } = string.Empty;

    [StringLength(14)]
    public string? CPF { get; set; }

    [EmailAddress]
    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(9)]
    public string? CEP { get; set; }

    [StringLength(200)]
    public string? Endereco { get; set; }

    [StringLength(100)]
    public string? Cidade { get; set; }

    [StringLength(2, MinimumLength = 2)]
    public string? UF { get; set; }
}
