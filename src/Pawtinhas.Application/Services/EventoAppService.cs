using Pawtinhas.Application.DTOs.Eventos;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Services;

public class EventoAppService
{
    private readonly IEventoRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public EventoAppService(
        IEventoRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EventoResponseDto> CadastrarAsync(
        CadastrarEventoDto dto)
    {
        if (!await _repository.AnimalExisteAsync(dto.AnimalId))
            throw new KeyNotFoundException("Animal não encontrado.");

        if (dto.DataHora == default)
            throw new ArgumentException("A data e hora são obrigatórias.");

        Evento evento = dto.TipoEvento.Trim().ToLowerInvariant() switch
        {
            "consulta" => new Consulta(
                dto.AnimalId,
                dto.DataHora,
                Exigir(dto.Motivo, "Motivo"),
                dto.Observacoes),

            "cirurgia" => new Cirurgia(
                dto.AnimalId,
                dto.DataHora,
                Exigir(dto.Procedimento, "Procedimento"),
                dto.Observacoes),

            "banhotosa" or "banho e tosa" => new BanhoTosa(
                dto.AnimalId,
                dto.DataHora,
                Exigir(dto.TipoServico, "Tipo de serviço"),
                dto.Observacoes),

            "petsitter" or "pet sitter" => CriarPetSitter(dto),

            "emergencia" or "emergência" => new Emergencia(
                dto.AnimalId,
                dto.DataHora,
                Exigir(dto.Motivo, "Motivo"),
                Exigir(dto.Prioridade, "Prioridade"),
                dto.Observacoes),

            "vacina" => new Vacina(
                dto.AnimalId,
                dto.DataHora,
                Exigir(dto.NomeVacina, "Nome da vacina"),
                Exigir(dto.Dose, "Dose"),
                dto.Observacoes),

            _ => throw new ArgumentException(
                "Tipo de evento inválido.")
        };

        await _repository.AdicionarAsync(evento);
        await _unitOfWork.CommitAsync();

        return EventoResponseDto.FromEntity(evento);
    }

    public async Task<IEnumerable<EventoResponseDto>> ListarAsync()
    {
        var eventos = await _repository.ObterTodosAsync();

        return eventos.Select(EventoResponseDto.FromEntity);
    }

    public async Task<EventoResponseDto> ObterPorIdAsync(int id)
    {
        var evento = await _repository.ObterPorIdAsync(id);

        if (evento is null)
            throw new KeyNotFoundException("Evento não encontrado.");

        return EventoResponseDto.FromEntity(evento);
    }

    private static PetSitter CriarPetSitter(CadastrarEventoDto dto)
    {
        if (dto.DataHoraFim is null)
            throw new ArgumentException(
                "A data e hora final são obrigatórias para Pet Sitter.");

        if (dto.DataHoraFim <= dto.DataHora)
            throw new ArgumentException(
                "O término deve ser posterior ao início.");

        return new PetSitter(
            dto.AnimalId,
            dto.DataHora,
            dto.DataHoraFim.Value,
            Exigir(dto.Local, "Local"),
            dto.Observacoes);
    }

    private static string Exigir(string? valor, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException($"{campo} é obrigatório.");

        return valor.Trim();
    }
}