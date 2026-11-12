using Pawtinhas.Application.DTOs.Eventos;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Comum;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Services;

/// <summary>Agenda da clínica: consultas, vacinas, cirurgias, banho e tosa, pet sitter e emergências.</summary>
public class EventoAppService
{
    private readonly IEventoRepository _repository;
    private readonly IAnimalRepository _animais;
    private readonly IRepository<Profissional> _profissionais;
    private readonly IUnitOfWork _unitOfWork;

    public EventoAppService(
        IEventoRepository repository,
        IAnimalRepository animais,
        IRepository<Profissional> profissionais,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _animais = animais;
        _profissionais = profissionais;
        _unitOfWork = unitOfWork;
    }

    public async Task<EventoResponseDto> CadastrarAsync(
        CadastrarEventoDto dto)
    {
        var tipo = Normalizar(dto.TipoEvento);
        await ValidarReferenciasAsync(dto, tipo);

        if (dto.DataHora == default)
            throw new DominioException("dataHora", "A data e hora são obrigatórias.");

        Evento evento = tipo switch
        {
            "consulta" => new Consulta(
                dto.AnimalId!.Value,
                dto.DataHora,
                Exigir(dto.Motivo, "motivo", "Motivo"),
                dto.Observacoes),

            "cirurgia" => new Cirurgia(
                dto.AnimalId!.Value,
                dto.DataHora,
                Exigir(dto.Procedimento, "procedimento", "Procedimento"),
                dto.Observacoes),

            "banhotosa" => new BanhoTosa(
                dto.AnimalId!.Value,
                dto.DataHora,
                Exigir(dto.TipoServico, "tipoServico", "Tipo de serviço"),
                dto.Observacoes),

            "petsitter" => CriarPetSitter(dto),

            "emergencia" => new Emergencia(
                dto.AnimalId,
                dto.DataHora,
                Exigir(dto.Motivo, "motivo", "Motivo"),
                Exigir(dto.Prioridade, "prioridade", "Prioridade"),
                dto.Observacoes,
                dto.DescricaoAnimal),

            "vacina" => new Vacina(
                dto.AnimalId!.Value,
                dto.DataHora,
                Exigir(dto.NomeVacina, "nomeVacina", "Nome da vacina"),
                Exigir(dto.Dose, "dose", "Dose"),
                dto.Observacoes),

            _ => throw new DominioException("tipoEvento", "Tipo de evento inválido.")
        };

        evento.DefinirProfissional(dto.ProfissionalId);
        if (!string.IsNullOrWhiteSpace(dto.Status)) evento.AlterarStatus(dto.Status);

        await _repository.AdicionarAsync(evento);
        await _unitOfWork.CommitAsync();

        return await ObterPorIdAsync(evento.Id);
    }

    public async Task<EventoResponseDto> AtualizarAsync(int id, CadastrarEventoDto dto)
    {
        var evento = await _repository.ObterCompletoAsync(id)
            ?? throw new KeyNotFoundException("Evento não encontrado.");

        var tipo = Normalizar(dto.TipoEvento);
        if (Normalizar(evento.GetType().Name) != tipo)
            throw new DominioException("tipoEvento", "O tipo de um compromisso não pode ser trocado. Exclua e agende de novo.");

        await ValidarReferenciasAsync(dto, tipo);

        evento.Reagendar(dto.AnimalId, dto.DataHora, dto.ProfissionalId, dto.Observacoes);
        switch (evento)
        {
            case Consulta consulta:
                consulta.Atualizar(Exigir(dto.Motivo, "motivo", "Motivo"));
                break;
            case Cirurgia cirurgia:
                cirurgia.Atualizar(Exigir(dto.Procedimento, "procedimento", "Procedimento"));
                break;
            case BanhoTosa banho:
                banho.Atualizar(Exigir(dto.TipoServico, "tipoServico", "Tipo de serviço"));
                break;
            case PetSitter visita:
                visita.Atualizar(dto.DataHora, dto.DataHoraFim ?? dto.DataHora,
                    Exigir(dto.Local, "local", "Local"), Exigir(dto.Servico, "servico", "Serviço"),
                    Exigir(dto.Periodo, "periodo", "Período"));
                break;
            case Emergencia emergencia:
                emergencia.Atualizar(dto.AnimalId, Exigir(dto.Motivo, "motivo", "Motivo"),
                    Exigir(dto.Prioridade, "prioridade", "Prioridade"), dto.DescricaoAnimal);
                break;
            case Vacina vacina:
                vacina.Atualizar(Exigir(dto.NomeVacina, "nomeVacina", "Nome da vacina"), Exigir(dto.Dose, "dose", "Dose"));
                break;
        }

        if (!string.IsNullOrWhiteSpace(dto.Status)) evento.AlterarStatus(dto.Status);

        await _unitOfWork.CommitAsync();
        return await ObterPorIdAsync(id);
    }

    public async Task<EventoResponseDto> AlterarStatusAsync(int id, string status)
    {
        var evento = await _repository.ObterCompletoAsync(id)
            ?? throw new KeyNotFoundException("Evento não encontrado.");

        evento.AlterarStatus(status);
        await _unitOfWork.CommitAsync();
        return EventoResponseDto.FromEntity(evento);
    }

    /// <param name="tipo">Filtro opcional: consulta, vacina, cirurgia, banhotosa, petsitter ou emergencia.</param>
    public async Task<IEnumerable<EventoResponseDto>> ListarAsync(string? tipo = null, int? animalId = null)
    {
        var classe = string.IsNullOrWhiteSpace(tipo) ? null : Classe(Normalizar(tipo))
            ?? throw new DominioException("tipo", "Tipo de evento inválido.");

        var eventos = await _repository.ListarAsync(classe, animalId);

        return eventos.Select(EventoResponseDto.FromEntity);
    }

    public async Task<EventoResponseDto> ObterPorIdAsync(int id)
    {
        var evento = await _repository.ObterCompletoAsync(id);

        if (evento is null)
            throw new KeyNotFoundException("Evento não encontrado.");

        return EventoResponseDto.FromEntity(evento);
    }

    public async Task ExcluirAsync(int id)
    {
        var evento = await _repository.ObterPorIdAsync(id)
            ?? throw new KeyNotFoundException("Evento não encontrado.");

        _repository.Remover(evento);
        await _unitOfWork.CommitAsync();
    }

    private async Task ValidarReferenciasAsync(CadastrarEventoDto dto, string tipo)
    {
        if (dto.AnimalId is { } animalId)
        {
            var animal = await _animais.ObterPorIdAsync(animalId);
            if (animal is null || !animal.EhPaciente)
                throw new DominioException("animalId", "Paciente não encontrado. Selecione outro.");
        }
        else if (tipo != "emergencia")
        {
            throw new DominioException("animalId", "Selecione o paciente.");
        }

        if (dto.ProfissionalId is { } profissionalId && await _profissionais.ObterPorIdAsync(profissionalId) is null)
            throw new DominioException("profissionalId", "Profissional não encontrado. Selecione outro.");
    }

    private static PetSitter CriarPetSitter(CadastrarEventoDto dto)
    {
        if (dto.DataHoraFim is null)
            throw new DominioException("dataHoraFim",
                "A data final é obrigatória para Pet Sitter.");

        return new PetSitter(
            dto.AnimalId!.Value,
            dto.DataHora,
            dto.DataHoraFim.Value,
            Exigir(dto.Local, "local", "Local"),
            dto.Observacoes,
            Exigir(dto.Servico, "servico", "Serviço"),
            Exigir(dto.Periodo, "periodo", "Período"));
    }

    /// <summary>Aceita as grafias que a equipe já usava ("Banho e Tosa", "pet sitter", "emergência").</summary>
    private static string Normalizar(string? tipo) => (tipo ?? "").Trim().ToLowerInvariant() switch
    {
        "banho e tosa" or "banhotosa" => "banhotosa",
        "pet sitter" or "petsitter" => "petsitter",
        "emergência" or "emergencia" => "emergencia",
        var t => t,
    };

    private static Type? Classe(string tipo) => tipo switch
    {
        "consulta" => typeof(Consulta),
        "cirurgia" => typeof(Cirurgia),
        "banhotosa" => typeof(BanhoTosa),
        "petsitter" => typeof(PetSitter),
        "emergencia" => typeof(Emergencia),
        "vacina" => typeof(Vacina),
        _ => null,
    };

    private static string Exigir(string? valor, string campo, string nome)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new DominioException(campo, $"Preencha o campo “{nome}”.");

        return valor.Trim();
    }
}
