using Pawtinhas.Domain;

namespace Pawtinhas.Application.DTOs.Eventos;

public class EventoResponseDto
{
    public int Id { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    public int AnimalId { get; set; }
    public DateTime DataHora { get; set; }
    public string? Observacoes { get; set; }
    public string Status { get; set; } = string.Empty;

    public string? Motivo { get; set; }
    public string? Procedimento { get; set; }
    public string? TipoServico { get; set; }
    public DateTime? DataHoraFim { get; set; }
    public string? Local { get; set; }
    public string? Prioridade { get; set; }
    public string? NomeVacina { get; set; }
    public string? Dose { get; set; }

    public static EventoResponseDto FromEntity(Evento evento)
    {
        var response = new EventoResponseDto
        {
            Id = evento.Id,
            AnimalId = evento.AnimalId,
            DataHora = evento.DataHora,
            Observacoes = evento.Observacoes,
            Status = evento.Status,
            TipoEvento = evento.GetType().Name
        };

        switch (evento)
        {
            case Consulta consulta:
                response.Motivo = consulta.Motivo;
                break;

            case Cirurgia cirurgia:
                response.Procedimento = cirurgia.Procedimento;
                break;

            case BanhoTosa banhoTosa:
                response.TipoServico = banhoTosa.TipoServico;
                break;

            case PetSitter petSitter:
                response.DataHoraFim = petSitter.DataHoraFim;
                response.Local = petSitter.Local;
                break;

            case Emergencia emergencia:
                response.Motivo = emergencia.Motivo;
                response.Prioridade = emergencia.Prioridade;
                break;

            case Vacina vacina:
                response.NomeVacina = vacina.NomeVacina;
                response.Dose = vacina.Dose;
                break;
        }

        return response;
    }
}