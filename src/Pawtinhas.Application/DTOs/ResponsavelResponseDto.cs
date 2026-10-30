
using Pawtinhas.Domain;

namespace Pawtinhas.Application.DTOs;

public class ResponsavelResponseDto
{
    public int Id { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string? CPF { get; set; }
    public string Telefone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? CEP { get; set; }
    public string? Endereco { get; set; }
    public string? Cidade { get; set; }
    public string? UF { get; set; }

    public static ResponsavelResponseDto FromEntity(Responsavel responsavel)
    {
        return new ResponsavelResponseDto
        {
            Id = responsavel.Id,
            NomeCompleto = responsavel.NomeCompleto,
            CPF = responsavel.CPF,
            Telefone = responsavel.Telefone,
            Email = responsavel.Email,
            CEP = responsavel.CEP,
            Endereco = responsavel.Endereco,
            Cidade = responsavel.Cidade,
            UF = responsavel.UF
        };
    }
}
