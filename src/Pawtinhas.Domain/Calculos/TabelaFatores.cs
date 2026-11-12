namespace Pawtinhas.Domain.Calculos;

/// <summary>
/// Fator multiplicador da necessidade energética de repouso (MER = RER × fator).
/// <see cref="FatorMin"/> e <see cref="FatorMax"/> são a faixa publicada; <see cref="Fator"/> é o valor usado
/// quando a veterinária não escolhe outro dentro da faixa.
/// </summary>
public sealed record FatorEnergetico(
    string Chave,
    string Especie,
    string Rotulo,
    decimal Fator,
    decimal FatorMin,
    decimal FatorMax,
    bool UsaPesoMeta,
    string Fonte);

/// <summary>
/// Fatores de manutenção para cães e gatos. Fontes:
/// Merck Veterinary Manual, tabela "Daily Maintenance Energy Requirements for Dogs and Cats";
/// AAHA 2021 Nutrition and Weight Management Guidelines for Dogs and Cats, Box 1 ("Energy Requirement Calculations").
/// Os próprios guias lembram que o resultado é um ponto de partida, ajustado pelo peso e pelo escore corporal.
/// </summary>
public static class TabelaFatores
{
    public const string Personalizado = "personalizado";

    private const string Merck = "Merck Veterinary Manual (Daily Maintenance Energy Requirements for Dogs and Cats)";
    private const string Aaha = "AAHA 2021 Nutrition and Weight Management Guidelines, Box 1";
    private const string Ambos = Merck + "; " + Aaha;

    public static readonly IReadOnlyList<FatorEnergetico> Todos =
    [
        new("cao_filhote_ate_4m", "Cachorro", "Filhote até 4 meses", 3.0m, 3.0m, 3.0m, false, Ambos),
        new("cao_filhote_4m_adulto", "Cachorro", "Filhote de 4 meses até a fase adulta", 2.0m, 2.0m, 2.0m, false, Ambos),
        new("cao_adulto_castrado", "Cachorro", "Adulto castrado", 1.6m, 1.4m, 1.6m, false, Ambos),
        new("cao_adulto_inteiro", "Cachorro", "Adulto não castrado", 1.8m, 1.6m, 1.8m, false, Ambos),
        new("cao_propenso_obesidade", "Cachorro", "Adulto inativo ou propenso à obesidade", 1.2m, 1.0m, 1.4m, false,
            "AAHA 2021 (1,0 a 1,2); Merck (1,4)"),
        new("cao_perda_peso", "Cachorro", "Perda de peso (calculado no peso ideal)", 1.0m, 1.0m, 1.0m, true, Aaha),
        new("cao_gestacao", "Cachorro", "Gestação (últimos 21 dias)", 3.0m, 3.0m, 3.0m, false, Aaha),
        new("cao_lactacao", "Cachorro", "Lactação (ajuste pelo tamanho da ninhada)", 3.0m, 3.0m, 6.0m, false, Aaha),
        new("cao_trabalho_leve", "Cachorro", "Trabalho leve", 1.6m, 1.6m, 2.0m, false, Aaha),
        new("cao_trabalho_moderado", "Cachorro", "Trabalho moderado", 2.0m, 2.0m, 5.0m, false, Aaha),

        new("gato_filhote", "Gato", "Filhote", 2.5m, 2.5m, 2.5m, false, Ambos),
        new("gato_adulto_castrado", "Gato", "Adulto castrado", 1.2m, 1.2m, 1.4m, false, Ambos),
        new("gato_adulto_inteiro", "Gato", "Adulto não castrado", 1.4m, 1.4m, 1.6m, false, Ambos),
        new("gato_propenso_obesidade", "Gato", "Adulto inativo ou propenso à obesidade", 1.0m, 1.0m, 1.0m, false, Ambos),
        new("gato_perda_peso", "Gato", "Perda de peso (calculado no peso ideal)", 0.8m, 0.8m, 0.8m, true, Aaha),
        new("gato_gestacao", "Gato", "Gestação", 1.6m, 1.6m, 2.0m, false, Aaha),
        new("gato_lactacao", "Gato", "Lactação (ajuste pelo tamanho da ninhada)", 2.0m, 2.0m, 6.0m, false, Aaha),
    ];

    public static FatorEnergetico? Buscar(string? chave) => Todos.FirstOrDefault(f => f.Chave == chave);
}
