
using Microsoft.EntityFrameworkCore;
using Pawtinhas.Domain;

namespace Pawtinhas.Infrastructure.Data;

public class AppDbContext(
    DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<Animal> Animais { get; set; } = null!;
    public DbSet<Responsavel> Responsaveis { get; set; } = null!;

    public DbSet<Evento> Eventos { get; set; } = null!;
    public DbSet<Consulta> Consultas { get; set; } = null!;
    public DbSet<Cirurgia> Cirurgias { get; set; } = null!;
    public DbSet<BanhoTosa> BanhosTosas { get; set; } = null!;
    public DbSet<PetSitter> PetSitters { get; set; } = null!;
    public DbSet<Emergencia> Emergencias { get; set; } = null!;
    public DbSet<Vacina> Vacinas { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}