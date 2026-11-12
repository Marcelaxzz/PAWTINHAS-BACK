using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pawtinhas.Domain;

namespace Pawtinhas.Infrastructure.Data.Configuration;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.Property(u => u.Nome).IsRequired().HasMaxLength(80);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(254);
        builder.Property(u => u.SenhaHash).IsRequired().HasMaxLength(200);
        builder.HasIndex(u => u.Email).IsUnique();
    }
}

public class DesafioCodigoConfiguration : IEntityTypeConfiguration<DesafioCodigo>
{
    public void Configure(EntityTypeBuilder<DesafioCodigo> builder)
    {
        builder.ToTable("DesafiosCodigo");
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.CodigoHash).IsRequired().HasMaxLength(64);
        builder.Property(d => d.SenhaPendenteHash).HasMaxLength(200);
        builder.HasIndex(d => d.ExpiraEm);
        builder.HasIndex(d => new { d.UsuarioId, d.Finalidade, d.CriadoEm });
        builder.HasOne<Usuario>().WithMany().HasForeignKey(d => d.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SessaoUsuarioConfiguration : IEntityTypeConfiguration<SessaoUsuario>
{
    public void Configure(EntityTypeBuilder<SessaoUsuario> builder)
    {
        builder.ToTable("SessoesUsuario");
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(s => s.TokenHash).IsUnique();
        builder.HasIndex(s => s.Familia);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(s => s.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
