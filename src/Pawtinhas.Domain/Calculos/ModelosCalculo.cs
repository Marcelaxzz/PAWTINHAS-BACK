namespace Pawtinhas.Domain.Calculos;

/// <param name="FatorPersonalizado">
/// Na condição "personalizado", o fator escolhido (0,5 a 8). Nas demais, um valor dentro da faixa
/// publicada para a condição (ex.: lactação 3,0 a 6,0); nulo usa o valor padrão da tabela.
/// </param>
public sealed record EntradaAlimentacao(
    string Especie,
    decimal PesoAtualKg,
    decimal? PesoMetaKg,
    string Condicao,
    decimal? FatorPersonalizado,
    decimal KcalPorKg,
    int Refeicoes);

public sealed record ResultadoAlimentacao(
    decimal PesoCalculoKg,
    decimal Fator,
    string CondicaoRotulo,
    decimal Rer,
    decimal Mer,
    decimal GramasDia,
    decimal GramasPorRefeicao,
    decimal KcalPetiscosMax,
    decimal AguaMinMlDia,
    decimal AguaMaxMlDia,
    IReadOnlyList<string> Fontes);

public sealed record EntradaDose(
    string Especie,
    decimal PesoKg,
    decimal DoseMgKg,
    decimal Concentracao,
    string Unidade,
    bool Fracionavel,
    int IntervaloHoras,
    int DuracaoDias,
    decimal? DoseMinMgKg,
    decimal? DoseMaxMgKg,
    decimal? DoseMaximaMg);

public sealed record ResultadoDose(
    decimal DoseMg,
    decimal QuantidadeExata,
    decimal QuantidadePorDose,
    decimal DoseRealMg,
    decimal DoseRealMgKg,
    decimal DesvioPercentual,
    decimal DosesPorDia,
    int TotalDoses,
    decimal QuantidadeTotal,
    decimal? DoseMinMgKg,
    decimal? DoseMaxMgKg,
    IReadOnlyList<string> Alertas,
    IReadOnlyList<string> Avisos);
