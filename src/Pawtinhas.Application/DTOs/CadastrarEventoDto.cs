using System.ComponentModel.DataAnnotations;

namespace Pawtinhas.Application.DTOs.Eventos;

/// <summary>
/// Cadastro e edição de compromisso. <see cref="TipoEvento"/>: consulta, vacina, cirurgia, banhotosa,
/// petsitter ou emergencia. Cada tipo usa os campos dele; os outros são ignorados.
/// </summary>
public class CadastrarEventoDto
{
    [Required(ErrorMessage = "Informe o tipo do compromisso.")]
    public string TipoEvento { get; set; } = string.Empty;

    /// <summary>Obrigatório, menos na emergência de animal não cadastrado (aí vale <see cref="DescricaoAnimal"/>).</summary>
    public int? AnimalId { get; set; }

    [Required(ErrorMessage = "Informe a data e o horário.")]
    public DateTime DataHora { get; set; }

    [StringLength(1000)]
    public string? Observacoes { get; set; }

    public int? ProfissionalId { get; set; }

    /// <summary>Na edição; no cadastro vale o status inicial do tipo.</summary>
    [StringLength(30)]
    public string? Status { get; set; }

    [StringLength(200)]
    public string? Motivo { get; set; }

    [StringLength(120)]
    public string? Procedimento { get; set; }

    [StringLength(80)]
    public string? TipoServico { get; set; }

    public DateTime? DataHoraFim { get; set; }

    [StringLength(160)]
    public string? Local { get; set; }

    [StringLength(80)]
    public string? Servico { get; set; }

    [StringLength(20)]
    public string? Periodo { get; set; }

    [StringLength(20)]
    public string? Prioridade { get; set; }

    [StringLength(120)]
    public string? DescricaoAnimal { get; set; }

    [StringLength(60)]
    public string? NomeVacina { get; set; }

    [StringLength(20)]
    public string? Dose { get; set; }
}

public class AlterarStatusEventoDto
{
    [Required(ErrorMessage = "Informe o status.")]
    [StringLength(30)]
    public string Status { get; set; } = string.Empty;
}
