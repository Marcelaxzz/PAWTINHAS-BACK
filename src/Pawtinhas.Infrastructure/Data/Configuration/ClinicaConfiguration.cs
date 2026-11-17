using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pawtinhas.Domain;

namespace Pawtinhas.Infrastructure.Data.Configuration;

public class ProfissionalConfiguration : IEntityTypeConfiguration<Profissional>
{
    public void Configure(EntityTypeBuilder<Profissional> builder)
    {
        builder.ToTable("Profissionais");
        builder.Property(p => p.Nome).IsRequired().HasMaxLength(80);
        builder.Property(p => p.Funcao).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Email).HasMaxLength(150);
        builder.Ignore(p => p.Papel);
        builder.Ignore(p => p.Iniciais);
    }
}

public class AlarmeConfiguration : IEntityTypeConfiguration<Alarme>
{
    public void Configure(EntityTypeBuilder<Alarme> builder)
    {
        builder.ToTable("Alarmes");
        builder.Property(a => a.Nome).IsRequired().HasMaxLength(60);
        builder.Property(a => a.Descricao).IsRequired().HasMaxLength(140);
        builder.Property(a => a.Recorrencia).IsRequired().HasMaxLength(30);
        builder.HasOne(a => a.Animal).WithMany().HasForeignKey(a => a.AnimalId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class DisparoAlarmeConfiguration : IEntityTypeConfiguration<DisparoAlarme>
{
    public void Configure(EntityTypeBuilder<DisparoAlarme> builder)
    {
        builder.ToTable("DisparosAlarme");
        builder.Property(d => d.Nome).IsRequired().HasMaxLength(60);
        builder.Property(d => d.Resultado).IsRequired().HasMaxLength(60);
        builder.HasIndex(d => d.Quando);
        // O histórico sobrevive à exclusão do alarme (fica só com o nome).
        builder.HasOne<Alarme>().WithMany().HasForeignKey(d => d.AlarmeId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class MedicamentoConfiguration : IEntityTypeConfiguration<Medicamento>
{
    public void Configure(EntityTypeBuilder<Medicamento> builder)
    {
        builder.ToTable("Medicamentos");
        builder.Property(m => m.Nome).IsRequired().HasMaxLength(80);
        builder.Property(m => m.PrincipioAtivo).IsRequired().HasMaxLength(80);
        builder.Property(m => m.Forma).IsRequired().HasMaxLength(40);
        builder.Property(m => m.Unidade).IsRequired().HasMaxLength(20);
        builder.Property(m => m.Via).IsRequired().HasMaxLength(20);
        builder.Property(m => m.Observacoes).IsRequired().HasMaxLength(500);
        builder.Property(m => m.Fonte).IsRequired().HasMaxLength(300);
        builder.Property(m => m.Concentracao).HasPrecision(12, 4);
        builder.Property(m => m.DoseMinCao).HasPrecision(10, 4);
        builder.Property(m => m.DoseMaxCao).HasPrecision(10, 4);
        builder.Property(m => m.DoseMinGato).HasPrecision(10, 4);
        builder.Property(m => m.DoseMaxGato).HasPrecision(10, 4);
        builder.Property(m => m.DoseMaximaMg).HasPrecision(12, 4);
    }
}

public class RacaoConfiguration : IEntityTypeConfiguration<Racao>
{
    public void Configure(EntityTypeBuilder<Racao> builder)
    {
        builder.ToTable("Racoes");
        builder.Property(r => r.Nome).IsRequired().HasMaxLength(80);
        builder.Property(r => r.Especie).IsRequired().HasMaxLength(20);
        builder.Property(r => r.Fase).IsRequired().HasMaxLength(20);
        builder.Property(r => r.Tipo).IsRequired().HasMaxLength(20);
        builder.Property(r => r.Observacoes).IsRequired().HasMaxLength(300);
        builder.Property(r => r.KcalPorKg).HasPrecision(8, 2);
        builder.Property(r => r.FotoUrl).HasMaxLength(500);
    }
}

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("Materiais");
        builder.Property(m => m.Nome).IsRequired().HasMaxLength(80);
        builder.Property(m => m.Tipo).IsRequired().HasMaxLength(20);
        builder.Property(m => m.Unidade).IsRequired().HasMaxLength(20);
        builder.Property(m => m.Especificacao).IsRequired().HasMaxLength(80);
        builder.Property(m => m.Observacoes).IsRequired().HasMaxLength(300);
    }
}

public class PrescricaoConfiguration : IEntityTypeConfiguration<Prescricao>
{
    public void Configure(EntityTypeBuilder<Prescricao> builder)
    {
        builder.ToTable("Prescricoes");
        builder.Property(p => p.Medicamento).IsRequired().HasMaxLength(80);
        builder.Property(p => p.Unidade).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Via).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Status).IsRequired().HasMaxLength(20);
        builder.Property(p => p.AlertasConfirmados).IsRequired().HasMaxLength(1000);
        builder.Property(p => p.Observacoes).IsRequired().HasMaxLength(500);
        builder.Property(p => p.Concentracao).HasPrecision(12, 4);
        builder.Property(p => p.PesoKg).HasPrecision(6, 2);
        builder.Property(p => p.DoseMgKg).HasPrecision(10, 4);
        builder.Property(p => p.DoseMg).HasPrecision(12, 3);
        builder.Property(p => p.QuantidadePorDose).HasPrecision(10, 2);
        builder.Property(p => p.DoseRealMg).HasPrecision(12, 3);
        builder.Property(p => p.QuantidadeTotal).HasPrecision(12, 2);
        builder.HasOne<Animal>().WithMany().HasForeignKey(p => p.AnimalId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Medicamento>().WithMany().HasForeignKey(p => p.MedicamentoId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Alarme>().WithMany().HasForeignKey(p => p.AlarmeId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PlanoAlimentarConfiguration : IEntityTypeConfiguration<PlanoAlimentar>
{
    public void Configure(EntityTypeBuilder<PlanoAlimentar> builder)
    {
        builder.ToTable("PlanosAlimentares");
        builder.Property(p => p.Racao).IsRequired().HasMaxLength(80);
        builder.Property(p => p.Especie).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Condicao).IsRequired().HasMaxLength(80);
        builder.Property(p => p.Observacoes).IsRequired().HasMaxLength(500);
        builder.Property(p => p.KcalPorKg).HasPrecision(8, 2);
        builder.Property(p => p.PesoAtualKg).HasPrecision(6, 2);
        builder.Property(p => p.PesoCalculoKg).HasPrecision(6, 2);
        builder.Property(p => p.Fator).HasPrecision(4, 2);
        builder.Property(p => p.Rer).HasPrecision(8, 1);
        builder.Property(p => p.Mer).HasPrecision(8, 1);
        builder.Property(p => p.GramasDia).HasPrecision(8, 1);
        builder.Property(p => p.GramasPorRefeicao).HasPrecision(8, 1);
        builder.Property(p => p.KcalPetiscosMax).HasPrecision(8, 1);
        builder.Property(p => p.AguaMinMlDia).HasPrecision(8, 1);
        builder.Property(p => p.AguaMaxMlDia).HasPrecision(8, 1);
        builder.HasOne<Animal>().WithMany().HasForeignKey(p => p.AnimalId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Racao>().WithMany().HasForeignKey(p => p.RacaoId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ConfiguracaoClinicaConfiguration : IEntityTypeConfiguration<ConfiguracaoClinica>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoClinica> builder)
    {
        builder.ToTable("ConfiguracoesClinica");
        builder.Property(c => c.NomeClinica).IsRequired().HasMaxLength(80);
        builder.Property(c => c.Endereco).IsRequired().HasMaxLength(160);
        builder.Property(c => c.Telefone).IsRequired().HasMaxLength(20);
        builder.Property(c => c.Cnpj).IsRequired().HasMaxLength(18);
    }
}
