using Microsoft.EntityFrameworkCore;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Interfaces;
using Pawtinhas.Infrastructure.Data;

namespace Pawtinhas.Infrastructure.Repositories;

public class EventoRepository
    : RepositoryBase<Evento>, IEventoRepository
{
    public EventoRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<bool> AnimalExisteAsync(int animalId)
    {
        return await _context.Animais
            .AnyAsync(animal => animal.Id == animalId);
    }

    public async Task<IReadOnlyList<Evento>> ListarAsync(Type? tipo, int? animalId)
    {
        var consulta = Completo();
        if (tipo is not null) consulta = consulta.Where(e => EF.Property<string>(e, "Discriminator") == tipo.Name);
        if (animalId is { } id) consulta = consulta.Where(e => e.AnimalId == id);
        return await consulta.OrderBy(e => e.DataHora).ToListAsync();
    }

    public Task<Evento?> ObterCompletoAsync(int id) => Completo().FirstOrDefaultAsync(e => e.Id == id);

    public async Task<IReadOnlyList<Evento>> ListarDoDiaAsync(DateOnly dia)
    {
        var inicio = dia.ToDateTime(TimeOnly.MinValue);
        var fim = inicio.AddDays(1);
        var doDia = await Completo().Where(e => e.DataHora >= inicio && e.DataHora < fim).ToListAsync();

        // Visitas de pet sitter cobrem vários dias, sempre no mesmo horário.
        var visitas = await Completo().OfType<PetSitter>()
            .Where(v => v.DataHora < inicio && v.DataHoraFim >= inicio)
            .ToListAsync();

        return [.. doDia, .. visitas];
    }

    private IQueryable<Evento> Completo() => _dbSet
        .Include(e => e.Animal!).ThenInclude(a => a.Responsavel)
        .Include(e => e.Profissional);
}
