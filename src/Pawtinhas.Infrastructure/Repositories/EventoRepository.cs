using Microsoft.EntityFrameworkCore;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Interfaces;
using Pawtinhas.Infrastructure.Data;

namespace Pawtinhas.Infrastructure.Repositories;

public class EventoRepository
    : RepositoryBase<Evento>, IEventoRepository
{
    private readonly AppDbContext _context;

    public EventoRepository(AppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<bool> AnimalExisteAsync(int animalId)
    {
        return await _context.Animais
            .AnyAsync(animal => animal.Id == animalId);
    }
}