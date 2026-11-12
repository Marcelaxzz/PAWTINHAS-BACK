using Pawtinhas.Domain;

namespace Pawtinhas.Application.DTOs.Eventos;

public class EventoResponseDto
{
    public int Id { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    public int? AnimalId { get; set; }
    public string? AnimalNome { get; set; }
    public string? AnimalFotoUrl { get; set; }
    public int? ResponsavelId { get; set; }
    public string? ResponsavelNome { get; set; }
    public int? ProfissionalId { get; set; }
    public string? ProfissionalNome { get; set; }
    public DateTime DataHora { get; set; }
    public string? Observacoes { get; set; }
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<string> StatusPermitidos { get; set; } = [];

    public string? Motivo { get; set; }
    public string? Procedimento { get; set; }
    public string? TipoServico { get; set; }
    public DateTime? DataHoraFim { get; set; }
    public string? Local { get; set; }
    public string? Servico { get; set; }
    public string? Periodo { get; set; }
    public string? Prioridade { get; set; }
    public string? DescricaoAnimal { get; set; }
    public string? NomeVacina { get; set; }
    public string? Dose { get; set; }
    public DateTime? AplicadaEm { get; set; }

    public static EventoResponseDto FromEntity(Evento evento)
    {
        var response = new EventoResponseDto
        {
            Id = evento.Id,
            AnimalId = evento.AnimalId,
            AnimalNome = evento.Animal?.Nome,
            AnimalFotoUrl = evento.Animal?.FotoUrl,
            ResponsavelId = evento.Animal?.ResponsavelId,
            ResponsavelNome = evento.Animal?.Responsavel?.NomeCompleto,
            ProfissionalId = evento.ProfissionalId,
            ProfissionalNome = evento.Profissional?.Nome,
            DataHora = evento.DataHora,
            Observacoes = evento.Observacoes,
            Status = evento.Status,
            StatusPermitidos = evento.StatusPermitidos,
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
                response.Servico = petSitter.Servico;
                response.Periodo = petSitter.Periodo;
                break;

            case Emergencia emergencia:
                response.Motivo = emergencia.Motivo;
                response.Prioridade = emergencia.Prioridade;
                response.DescricaoAnimal = emergencia.DescricaoAnimal;
                break;

            case Vacina vacina:
                response.NomeVacina = vacina.NomeVacina;
                response.Dose = vacina.Dose;
                response.AplicadaEm = vacina.AplicadaEm;
                break;
        }

        return response;
    }
}
