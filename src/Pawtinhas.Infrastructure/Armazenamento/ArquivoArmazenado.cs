using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pawtinhas.Infrastructure.Armazenamento;

/// <summary>
/// Cópia de cada foto enviada, guardada no banco. Em hospedagens sem disco permanente (Render
/// gratuito) a pasta de arquivos some a cada reinício; a foto volta do banco no primeiro pedido.
/// </summary>
public sealed class ArquivoArmazenado
{
    public string Nome { get; private set; } = string.Empty;
    public string TipoConteudo { get; private set; } = string.Empty;
    public byte[] Dados { get; private set; } = [];
    public DateTime CriadoEm { get; private set; }

    private ArquivoArmazenado() { }

    public ArquivoArmazenado(string nome, string tipoConteudo, byte[] dados, DateTime criadoEm)
    {
        Nome = nome;
        TipoConteudo = tipoConteudo;
        Dados = dados;
        CriadoEm = criadoEm;
    }
}

public sealed class ArquivoArmazenadoConfiguration : IEntityTypeConfiguration<ArquivoArmazenado>
{
    public void Configure(EntityTypeBuilder<ArquivoArmazenado> builder)
    {
        builder.ToTable("Arquivos");
        builder.HasKey(a => a.Nome);
        builder.Property(a => a.Nome).HasMaxLength(40);
        builder.Property(a => a.TipoConteudo).IsRequired().HasMaxLength(20);
        builder.Property(a => a.Dados).IsRequired().HasColumnType("longblob");
    }
}
