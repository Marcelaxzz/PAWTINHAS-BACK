
using Microsoft.EntityFrameworkCore;
using Pawtinhas.Domain;
using Pawtinhas.Infrastructure.Armazenamento;

namespace Pawtinhas.Infrastructure.Data;

public class AppDbContext(
    DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<Animal> Animais { get; set; } = null!;
    public DbSet<PerfilAdocao> PerfisAdocao { get; set; } = null!;
    public DbSet<Responsavel> Responsaveis { get; set; } = null!;

    public DbSet<Evento> Eventos { get; set; } = null!;
    public DbSet<Consulta> Consultas { get; set; } = null!;
    public DbSet<Cirurgia> Cirurgias { get; set; } = null!;
    public DbSet<BanhoTosa> BanhosTosas { get; set; } = null!;
    public DbSet<PetSitter> PetSitters { get; set; } = null!;
    public DbSet<Emergencia> Emergencias { get; set; } = null!;
    public DbSet<Vacina> Vacinas { get; set; } = null!;

    public DbSet<Profissional> Profissionais { get; set; } = null!;
    public DbSet<Alarme> Alarmes { get; set; } = null!;
    public DbSet<DisparoAlarme> DisparosAlarme { get; set; } = null!;
    public DbSet<Medicamento> Medicamentos { get; set; } = null!;
    public DbSet<Racao> Racoes { get; set; } = null!;
    public DbSet<Material> Materiais { get; set; } = null!;
    public DbSet<Prescricao> Prescricoes { get; set; } = null!;
    public DbSet<PlanoAlimentar> PlanosAlimentares { get; set; } = null!;
    public DbSet<ConfiguracaoClinica> Configuracoes { get; set; } = null!;
    public DbSet<ItemEstoque> ItensEstoque { get; set; } = null!;
    public DbSet<LoteEstoque> LotesEstoque { get; set; } = null!;
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque { get; set; } = null!;
    public DbSet<RelatorioServico> RelatoriosServico { get; set; } = null!;

    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<DesafioCodigo> DesafiosCodigo { get; set; } = null!;
    public DbSet<SessaoUsuario> Sessoes { get; set; } = null!;

    /// <summary>Cópia das fotos no banco (a pasta de arquivos pode não ser permanente).</summary>
    public DbSet<ArquivoArmazenado> Arquivos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
