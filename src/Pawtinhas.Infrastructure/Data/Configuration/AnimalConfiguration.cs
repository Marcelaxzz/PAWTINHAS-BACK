
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pawtinhas.Domain;

namespace Pawtinhas.Infrastructure.Data.Configurations;

public class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> builder)
    {
        builder.ToTable("Animais");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.Sexo)
            .IsRequired()
            .HasMaxLength(30);
        builder.Property(a => a.FotoUrl)
            .HasMaxLength(500);

        builder.Property(a => a.Localizacao)
            .HasMaxLength(200);

        builder.Property(a => a.Especie).HasMaxLength(40);
        builder.Property(a => a.Raca).HasMaxLength(60);
        builder.Property(a => a.Observacoes).HasMaxLength(1000);
        builder.Property(a => a.PesoKg).HasPrecision(6, 2);
        builder.HasIndex(a => a.Status);

        // Animal da vitrine pode não ter responsável ainda; paciente sempre tem.
        builder.HasOne(a => a.Responsavel)
            .WithMany(r => r.Animais)
            .HasForeignKey(a => a.ResponsavelId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.PerfilAdocao)
            .WithOne()
            .HasForeignKey<PerfilAdocao>(p => p.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PerfilAdocaoConfiguration : IEntityTypeConfiguration<PerfilAdocao>
{
    public void Configure(EntityTypeBuilder<PerfilAdocao> builder)
    {
        builder.ToTable("PerfisAdocao");
        builder.HasKey(p => p.AnimalId);
        builder.Property(p => p.AnimalId).ValueGeneratedNever();
        builder.Property(p => p.Descricao).IsRequired().HasMaxLength(600);
        builder.Property(p => p.Frase).IsRequired().HasMaxLength(140);
        builder.Property(p => p.Sobre).IsRequired().HasMaxLength(1200);
        builder.Property(p => p.Tracos).IsRequired().HasMaxLength(100);
        builder.Property(p => p.InfoAdicional).IsRequired().HasMaxLength(140);
        builder.Property(p => p.Necessidades).IsRequired().HasMaxLength(140);
    }
}
