using System.ComponentModel.DataAnnotations;

namespace Pawtinhas.Application.DTOs.Eventos;

public class CadastrarEventoDto
{
    [Required]
    public string TipoEvento { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int AnimalId { get; set; }

    [Required]
    public DateTime DataHora { get; set; }

    public string? Observacoes { get; set; }

    public string? Motivo { get; set; }

    public string? Procedimento { get; set; }

    public string? TipoServico { get; set; }

    public DateTime? DataHoraFim { get; set; }

    public string? Local { get; set; }

    public string? Prioridade { get; set; }

    public string? NomeVacina { get; set; }

    public string? Dose { get; set; }
}