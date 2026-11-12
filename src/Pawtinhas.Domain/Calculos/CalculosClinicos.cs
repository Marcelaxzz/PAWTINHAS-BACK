using System.Globalization;
using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain.Calculos;

/// <summary>Cálculos clínicos. Toda conta que vai para a ficha do paciente passa por aqui.</summary>
public static class CalculosClinicos
{
    public static readonly int[] Intervalos = [4, 6, 8, 12, 24, 48, 72];

    /// <summary>Desvio máximo aceito sem alerta entre a dose prescrita e a dose depois do arredondamento.</summary>
    private const decimal DesvioTolerado = 10m;

    /// <summary>Água de manutenção em ambiente termoneutro: 44 a 66 mL/kg/dia (Merck Veterinary Manual).</summary>
    private const decimal AguaMinMlKg = 44m;
    private const decimal AguaMaxMlKg = 66m;

    /// <summary>Petiscos e extras: no máximo 10% das calorias do dia (AAHA, manejo de peso).</summary>
    private const decimal FracaoPetiscos = 0.10m;

    public const string FonteRer = "RER = 70 × peso(kg)^0,75: Merck Veterinary Manual; AAHA 2021, Box 1";
    public const string FonteAgua = "Água: 44 a 66 mL/kg/dia em ambiente termoneutro (Merck Veterinary Manual)";
    public const string FontePetiscos = "Petiscos: até 10% das calorias diárias (AAHA, Weight Management)";

    private static readonly CultureInfo PtBr = new("pt-BR");

    // ---------- Alimentação ----------

    /// <summary>Necessidade energética de repouso: RER = 70 × peso(kg)^0,75 (kcal/dia).</summary>
    public static decimal Rer(decimal pesoKg) => (decimal)(70 * Math.Pow((double)pesoKg, 0.75));

    /// <summary>Valida e calcula. Lança <see cref="ValidacaoException"/> com os erros por campo.</summary>
    public static ResultadoAlimentacao Alimentacao(EntradaAlimentacao e)
    {
        var erros = new Dictionary<string, string>();
        if (e.Especie is not ("Cachorro" or "Gato"))
            erros["animalId"] = "O cálculo de ração está disponível para cães e gatos.";
        if (e.PesoAtualKg is < 0.1m or > 150m)
            erros["pesoAtualKg"] = "Informe o peso atual entre 0,1 e 150 kg.";
        if (e.KcalPorKg is < 300m or > 6000m)
            erros["kcalPorKg"] = "Informe a energia da ração entre 300 e 6.000 kcal/kg (veja o rótulo).";
        if (e.Refeicoes is < 1 or > 6)
            erros["refeicoes"] = "Divida em 1 a 6 refeições por dia.";

        decimal fator = 0;
        var rotulo = "";
        var fontes = new List<string> { FonteRer };
        var pesoCalculo = e.PesoAtualKg;

        if (e.Condicao == TabelaFatores.Personalizado)
        {
            if (e.FatorPersonalizado is { } f && f >= 0.5m && f <= 8m) fator = f;
            else erros["fatorPersonalizado"] = "Informe um fator entre 0,5 e 8.";
            rotulo = "Fator definido pela veterinária";
            if (e.PesoMetaKg is { } meta) pesoCalculo = meta;
        }
        else if (TabelaFatores.Buscar(e.Condicao) is { } condicao && condicao.Especie == e.Especie)
        {
            fator = condicao.Fator;
            rotulo = condicao.Rotulo;
            fontes.Add($"Fator: {condicao.Fonte}");
            if (e.FatorPersonalizado is { } ajuste)
            {
                if (ajuste < condicao.FatorMin || ajuste > condicao.FatorMax)
                    erros["fatorPersonalizado"] = condicao.FatorMin == condicao.FatorMax
                        ? $"Para esta condição a fonte indica fator {Num(condicao.Fator)}."
                        : $"Para esta condição use um fator entre {Num(condicao.FatorMin)} e {Num(condicao.FatorMax)}.";
                else fator = ajuste;
            }

            if (condicao.UsaPesoMeta)
            {
                if (e.PesoMetaKg is not { } meta) erros["pesoMetaKg"] = "Informe o peso ideal (meta) para esta condição.";
                else if (meta >= e.PesoAtualKg) erros["pesoMetaKg"] = "Para perda de peso, o peso ideal deve ser menor que o atual.";
                else pesoCalculo = meta;
            }
        }
        else
        {
            erros["condicao"] = "Selecione uma condição válida para a espécie.";
        }

        if (pesoCalculo is < 0.1m or > 150m) erros.TryAdd("pesoMetaKg", "Informe o peso ideal entre 0,1 e 150 kg.");
        ValidacaoException.SeHouver(erros);

        var rer = Rer(pesoCalculo);
        var mer = rer * fator;
        var gramasDia = mer / e.KcalPorKg * 1000m;
        fontes.Add(FonteAgua);
        fontes.Add(FontePetiscos);

        return new ResultadoAlimentacao(
            PesoCalculoKg: Math.Round(pesoCalculo, 2),
            Fator: fator,
            CondicaoRotulo: rotulo,
            Rer: Math.Round(rer, 0),
            Mer: Math.Round(mer, 0),
            GramasDia: Math.Round(gramasDia, 1),
            GramasPorRefeicao: Math.Round(gramasDia / e.Refeicoes, 1),
            KcalPetiscosMax: Math.Round(mer * FracaoPetiscos, 0),
            AguaMinMlDia: Math.Round(e.PesoAtualKg * AguaMinMlKg, 0),
            AguaMaxMlDia: Math.Round(e.PesoAtualKg * AguaMaxMlKg, 0),
            Fontes: fontes);
    }

    // ---------- Medicamentos ----------

    /// <summary>Dose = peso × mg/kg; quantidade = dose ÷ concentração, arredondada ao que dá para administrar.</summary>
    public static ResultadoDose Dose(EntradaDose e)
    {
        var erros = new Dictionary<string, string>();
        if (e.PesoKg is < 0.05m or > 150m) erros["pesoKg"] = "Informe o peso entre 0,05 e 150 kg.";
        if (e.DoseMgKg is <= 0m or > 1000m) erros["doseMgKg"] = "Informe a dose em mg/kg (maior que zero).";
        if (e.Concentracao is <= 0m or > 100000m) erros["concentracao"] = "Informe a concentração do medicamento.";
        if (!Medicamento.Unidades.Contains(e.Unidade)) erros["unidade"] = "Escolha a forma de administração.";
        if (!Intervalos.Contains(e.IntervaloHoras)) erros["intervaloHoras"] = "Escolha um intervalo válido.";
        if (e.DuracaoDias is < 1 or > 365) erros["duracaoDias"] = "Informe a duração entre 1 e 365 dias.";
        ValidacaoException.SeHouver(erros);

        var doseMg = e.PesoKg * e.DoseMgKg;
        var exata = doseMg / e.Concentracao;
        var porDose = Arredondar(exata, e.Unidade, e.Fracionavel);
        var doseReal = porDose * e.Concentracao;
        var doseRealMgKg = doseReal / e.PesoKg;
        var desvio = (doseReal - doseMg) / doseMg * 100m;
        var dosesPorDia = 24m / e.IntervaloHoras;
        var totalDoses = (int)Math.Ceiling(e.DuracaoDias * 24m / e.IntervaloHoras);
        var total = porDose * totalDoses;
        total = e.Unidade switch
        {
            // Comprimidos partidos: dispensa o número inteiro de comprimidos.
            "comprimido" => Math.Ceiling(total),
            "mL" => Math.Ceiling(total * 10m) / 10m,
            _ => total,
        };

        var alertas = new List<string>();
        var avisos = new List<string>();
        var (min, max) = (e.DoseMinMgKg, e.DoseMaxMgKg);
        var faixa = FormatarFaixa(min, max);

        if (min is null && max is null)
        {
            avisos.Add($"Sem faixa de referência cadastrada para {Plural(e.Especie)}: confira a dose na bula.");
        }
        else
        {
            if (min is { } a && e.DoseMgKg < a)
                alertas.Add($"Dose prescrita ({Num(e.DoseMgKg)} mg/kg) abaixo da faixa de referência ({faixa}).");
            if (max is { } b && e.DoseMgKg > b)
                alertas.Add($"Dose prescrita ({Num(e.DoseMgKg)} mg/kg) acima da faixa de referência ({faixa}).");
            var prescritaNaFaixa = (min is null || e.DoseMgKg >= min) && (max is null || e.DoseMgKg <= max);
            var realForaDaFaixa = (min is { } c && doseRealMgKg < c * 0.95m) || (max is { } d && doseRealMgKg > d * 1.05m);
            if (prescritaNaFaixa && realForaDaFaixa)
                alertas.Add($"Depois do arredondamento a dose fica em {Num(doseRealMgKg)} mg/kg, fora da faixa ({faixa}).");
        }

        if (e.DoseMaximaMg is { } teto && doseReal > teto)
            alertas.Add($"A dose de {Num(doseReal)} mg ultrapassa o máximo de {Num(teto)} mg por dose.");

        if (Math.Abs(desvio) > DesvioTolerado)
            alertas.Add($"O arredondamento para {Num(porDose)} {e.Unidade} muda a dose em {Num(Math.Round(desvio, 1))}%. "
                + "Considere outra apresentação ou concentração.");

        if (e.Unidade == "comprimido" && e.Fracionavel && exata < 0.25m)
            alertas.Add("A dose é menor que 1/4 de comprimido. Prefira uma apresentação líquida ou de menor concentração.");

        return new ResultadoDose(
            DoseMg: Math.Round(doseMg, 3),
            QuantidadeExata: Math.Round(exata, 3),
            QuantidadePorDose: porDose,
            DoseRealMg: Math.Round(doseReal, 3),
            DoseRealMgKg: Math.Round(doseRealMgKg, 3),
            DesvioPercentual: Math.Round(desvio, 1),
            DosesPorDia: Math.Round(dosesPorDia, 2),
            TotalDoses: totalDoses,
            QuantidadeTotal: Math.Round(total, 2),
            DoseMinMgKg: min,
            DoseMaxMgKg: max,
            Alertas: alertas,
            Avisos: avisos);
    }

    /// <summary>
    /// Quantidade administrável: 1/4 de comprimido sulcado, cápsula ou gota inteira,
    /// 0,01 mL abaixo de 1 mL (seringa de 1 mL) e 0,1 mL acima.
    /// </summary>
    public static decimal Arredondar(decimal quantidade, string unidade, bool fracionavel) => unidade switch
    {
        "comprimido" when fracionavel => Math.Max(0.25m, Math.Round(quantidade * 4m, MidpointRounding.AwayFromZero) / 4m),
        "comprimido" or "cápsula" or "gota" => Math.Max(1m, Math.Round(quantidade, MidpointRounding.AwayFromZero)),
        "mL" when quantidade < 1m => Math.Max(0.01m, Math.Round(quantidade, 2, MidpointRounding.AwayFromZero)),
        _ => Math.Round(quantidade, 1, MidpointRounding.AwayFromZero),
    };

    /// <summary>Faixa de referência do catálogo para a espécie do paciente.</summary>
    public static (decimal? Min, decimal? Max) FaixaPara(Medicamento m, string especie) => especie switch
    {
        "Cachorro" => (m.DoseMinCao, m.DoseMaxCao),
        "Gato" => (m.DoseMinGato, m.DoseMaxGato),
        _ => (null, null),
    };

    /// <summary>Recorrência equivalente do alarme do ESP32, quando existe.</summary>
    public static string? RecorrenciaAlarme(int intervaloHoras) => intervaloHoras switch
    {
        8 => "A cada 8 horas",
        12 => "A cada 12 horas",
        24 => "Diário (Todos os dias)",
        _ => null,
    };

    private static string FormatarFaixa(decimal? min, decimal? max) => (min, max) switch
    {
        ({ } a, { } b) when a == b => $"{Num(a)} mg/kg",
        ({ } a, { } b) => $"{Num(a)} a {Num(b)} mg/kg",
        ({ } a, null) => $"mínimo {Num(a)} mg/kg",
        (null, { } b) => $"máximo {Num(b)} mg/kg",
        _ => "",
    };

    private static string Plural(string especie) => especie switch
    {
        "Cachorro" => "cães",
        "Gato" => "gatos",
        _ => "esta espécie",
    };

    public static string Num(decimal valor) => valor.ToString("0.###", PtBr);
}
