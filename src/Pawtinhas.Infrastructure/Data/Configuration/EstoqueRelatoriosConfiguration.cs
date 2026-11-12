using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pawtinhas.Domain;

namespace Pawtinhas.Infrastructure.Data.Configuration;

public class ItemEstoqueConfiguration : IEntityTypeConfiguration<ItemEstoque>
{
    public void Configure(EntityTypeBuilder<ItemEstoque> builder)
    {
        builder.ToTable("ItensEstoque");
        builder.Property(i => i.Categoria).IsRequired().HasMaxLength(20);
        builder.Property(i => i.Minimo).HasPrecision(12, 3);
        builder.Ignore(i => i.Saldo);
        // Um registro por item do catálogo.
        builder.HasIndex(i => new { i.Categoria, i.ItemId }).IsUnique();
        builder.HasMany(i => i.Lotes).WithOne().HasForeignKey(l => l.ItemEstoqueId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class LoteEstoqueConfiguration : IEntityTypeConfiguration<LoteEstoque>
{
    public void Configure(EntityTypeBuilder<LoteEstoque> builder)
    {
        builder.ToTable("LotesEstoque");
        builder.Property(l => l.Codigo).IsRequired().HasMaxLength(40);
        builder.Property(l => l.Quantidade).HasPrecision(12, 3);
    }
}

public class MovimentacaoEstoqueConfiguration : IEntityTypeConfiguration<MovimentacaoEstoque>
{
    public void Configure(EntityTypeBuilder<MovimentacaoEstoque> builder)
    {
        builder.ToTable("MovimentacoesEstoque");
        builder.Property(m => m.Tipo).IsRequired().HasMaxLength(20);
        builder.Property(m => m.Motivo).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Lotes).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Quantidade).HasPrecision(12, 3);
        builder.Property(m => m.SaldoApos).HasPrecision(12, 3);
        builder.HasOne(m => m.Item).WithMany().HasForeignKey(m => m.ItemEstoqueId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(m => m.CriadoEm);
    }
}

public class RelatorioServicoConfiguration : IEntityTypeConfiguration<RelatorioServico>
{
    public void Configure(EntityTypeBuilder<RelatorioServico> builder)
    {
        builder.ToTable("RelatoriosServico");
        builder.Property(r => r.Queixa).IsRequired().HasMaxLength(1000);
        builder.Property(r => r.Avaliacao).IsRequired().HasMaxLength(2000);
        builder.Property(r => r.Procedimentos).IsRequired().HasMaxLength(2000);
        builder.Property(r => r.Diagnostico).IsRequired().HasMaxLength(1000);
        builder.Property(r => r.Orientacoes).IsRequired().HasMaxLength(2000);
        builder.Property(r => r.Responsavel).IsRequired().HasMaxLength(120);
        builder.Property(r => r.PesoKg).HasPrecision(6, 2);
        builder.Property(r => r.TemperaturaC).HasPrecision(4, 1);
        // Um relatório por atendimento; some junto com o atendimento.
        builder.HasIndex(r => r.EventoId).IsUnique();
        builder.HasOne(r => r.Evento).WithMany().HasForeignKey(r => r.EventoId).OnDelete(DeleteBehavior.Cascade);
    }
}
