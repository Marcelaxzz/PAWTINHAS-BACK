
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pawtinhas.Domain;

namespace Pawtinhas.Infrastructure.Data.Configuration;

public class ResponsavelConfiguration
    : IEntityTypeConfiguration<Responsavel>
{
    public void Configure(EntityTypeBuilder<Responsavel> builder)
    {
        builder.ToTable("Responsaveis");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.NomeCompleto)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(r => r.Telefone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(r => r.CPF)
            .HasMaxLength(14);

        builder.Property(r => r.Email)
            .HasMaxLength(150);

        builder.Property(r => r.CEP)
            .HasMaxLength(9);

        builder.Property(r => r.Endereco)
            .HasMaxLength(200);

        builder.Property(r => r.Cidade)
            .HasMaxLength(100);

        builder.Property(r => r.UF)
            .HasMaxLength(2);

        builder.Property(r => r.CriadoEm)
            .IsRequired();

        builder.HasIndex(r => r.CPF)
            .IsUnique();
    }
}
