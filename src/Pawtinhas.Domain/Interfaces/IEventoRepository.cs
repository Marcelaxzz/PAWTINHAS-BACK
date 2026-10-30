using Pawtinhas.Domain;

namespace Pawtinhas.Domain.Interfaces;

public interface IEventoRepository : IRepository<Evento>
{
    Task<bool> AnimalExisteAsync(int animalId);
}