using Pawtinhas.Domain.Calculos;
using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>Plano alimentar pela necessidade energética (RER × fator). Cada cálculo salvo vira um registro.</summary>
public class PlanoAlimentar
{
    public int Id { get; private set; }
    public int AnimalId { get; private set; }
    public int? RacaoId { get; private set; }
    public string Racao { get; private set; } = string.Empty;
    public decimal KcalPorKg { get; private set; }
    public string Especie { get; private set; } = string.Empty;
    public decimal PesoAtualKg { get; private set; }
    public decimal PesoCalculoKg { get; private set; }
    public string Condicao { get; private set; } = string.Empty;
    public decimal Fator { get; private set; }
    public decimal Rer { get; private set; }
    public decimal Mer { get; private set; }
    public int Refeicoes { get; private set; }
    public decimal GramasDia { get; private set; }
    public decimal GramasPorRefeicao { get; private set; }
    public decimal KcalPetiscosMax { get; private set; }
    public decimal AguaMinMlDia { get; private set; }
    public decimal AguaMaxMlDia { get; private set; }
    public string Observacoes { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }

    private PlanoAlimentar() { }

    public PlanoAlimentar(
        int animalId, int? racaoId, string racao, EntradaAlimentacao entrada, ResultadoAlimentacao resultado,
        string? observacoes, DateTime criadoEm)
    {
        AnimalId = animalId;
        RacaoId = racaoId;
        Racao = Validar.Texto(racao, "racao", 80, "Informe a ração.");
        KcalPorKg = entrada.KcalPorKg;
        Especie = entrada.Especie;
        PesoAtualKg = entrada.PesoAtualKg;
        PesoCalculoKg = resultado.PesoCalculoKg;
        Condicao = resultado.CondicaoRotulo.Length > 80 ? resultado.CondicaoRotulo[..80] : resultado.CondicaoRotulo;
        Fator = resultado.Fator;
        Rer = resultado.Rer;
        Mer = resultado.Mer;
        Refeicoes = entrada.Refeicoes;
        GramasDia = resultado.GramasDia;
        GramasPorRefeicao = resultado.GramasPorRefeicao;
        KcalPetiscosMax = resultado.KcalPetiscosMax;
        AguaMinMlDia = resultado.AguaMinMlDia;
        AguaMaxMlDia = resultado.AguaMaxMlDia;
        Observacoes = Validar.TextoLivre(observacoes, "observacoes", 500);
        CriadoEm = criadoEm;
    }
}
