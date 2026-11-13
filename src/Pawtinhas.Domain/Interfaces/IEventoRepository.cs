using Pawtinhas.Domain;

namespace Pawtinhas.Domain.Interfaces;

public interface IEventoRepository : IRepository<Evento>
{
    Task<bool> AnimalExisteAsync(int animalId);

    /// <summary>Compromissos com animal e responsável carregados, filtrados por tipo e/ou animal.</summary>
    Task<IReadOnlyList<Evento>> ListarAsync(Type? tipo, int? animalId);

    Task<Evento?> ObterCompletoAsync(int id);

    /// <summary>Compromissos de um dia (visitas de pet sitter que cobrem o dia também entram).</summary>
    Task<IReadOnlyList<Evento>> ListarDoDiaAsync(DateOnly dia);

        /// <summary>Compromissos dos animais de um tutor (nunca de outros), opcionalmente de um só animal.</summary>
    Task<IReadOnlyList<Evento>> ListarDoResponsavelAsync(int responsavelId, int? animalId);
}
