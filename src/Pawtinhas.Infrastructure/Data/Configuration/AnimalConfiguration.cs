
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

        builder.HasOne(a => a.Responsavel)
            .WithMany(r => r.Animais)
            .HasForeignKey(a => a.ResponsavelId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
