using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pawtinhas.Domain;

namespace Pawtinhas.Infrastructure.Data.Configuration;

/// <summary>Agenda em tabela única (TPH), como a equipe definiu: a coluna Discriminator guarda o tipo.</summary>
public class EventoConfiguration : IEntityTypeConfiguration<Evento>
{
    public void Configure(EntityTypeBuilder<Evento> builder)
    {
        builder.ToTable("Eventos");
        builder.HasKey(e => e.Id);
        builder.Ignore(e => e.StatusPermitidos);

        builder.Property(e => e.Status).IsRequired().HasMaxLength(30);
        builder.Property(e => e.Observacoes).HasMaxLength(1000);
        builder.HasIndex(e => e.DataHora);

        builder.HasOne(e => e.Animal)
            .WithMany()
            .HasForeignKey(e => e.AnimalId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Profissional)
            .WithMany()
            .HasForeignKey(e => e.ProfissionalId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ConsultaConfiguration : IEntityTypeConfiguration<Consulta>
{
    public void Configure(EntityTypeBuilder<Consulta> builder) =>
        builder.Property(c => c.Motivo).IsRequired().HasMaxLength(200).HasColumnName("Consulta_Motivo");
}

public class CirurgiaConfiguration : IEntityTypeConfiguration<Cirurgia>
{
    public void Configure(EntityTypeBuilder<Cirurgia> builder) =>
        builder.Property(c => c.Procedimento).IsRequired().HasMaxLength(120);
}

public class BanhoTosaConfiguration : IEntityTypeConfiguration<BanhoTosa>
{
    public void Configure(EntityTypeBuilder<BanhoTosa> builder) =>
        builder.Property(b => b.TipoServico).IsRequired().HasMaxLength(80);
}

public class VacinaConfiguration : IEntityTypeConfiguration<Vacina>
{
    public void Configure(EntityTypeBuilder<Vacina> builder)
    {
        builder.Property(v => v.NomeVacina).IsRequired().HasMaxLength(60);
        builder.Property(v => v.Dose).IsRequired().HasMaxLength(20);
    }
}

public class PetSitterConfiguration : IEntityTypeConfiguration<PetSitter>
{
    public void Configure(EntityTypeBuilder<PetSitter> builder)
    {
        builder.Property(p => p.Local).IsRequired().HasMaxLength(160);
        builder.Property(p => p.Servico).IsRequired().HasMaxLength(80);
        builder.Property(p => p.Periodo).IsRequired().HasMaxLength(20);
    }
}

public class EmergenciaConfiguration : IEntityTypeConfiguration<Emergencia>
{
    public void Configure(EntityTypeBuilder<Emergencia> builder)
    {
        builder.Property(e => e.Motivo).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Prioridade).IsRequired().HasMaxLength(20);
        builder.Property(e => e.DescricaoAnimal).HasMaxLength(120);
    }
}
