using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>Ração do catálogo. A energia metabolizável (kcal/kg) vem do rótulo do fabricante.</summary>
public class Racao
{
    public static readonly string[] Especies = ["Cachorro", "Gato"];
    public static readonly string[] Fases = ["Filhote", "Adulto", "Idoso", "Todas"];
    public static readonly string[] Tipos = ["Seca", "Úmida", "Terapêutica"];

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Especie { get; private set; } = string.Empty;
    public string Fase { get; private set; } = string.Empty;
    public string Tipo { get; private set; } = string.Empty;
    public decimal KcalPorKg { get; private set; }
    public string Observacoes { get; private set; } = string.Empty;
    /// <summary>Foto do pacote (endereço devolvido por /api/arquivos).</summary>
    public string? FotoUrl { get; private set; }

    private Racao() { }

    public Racao(string nome, string especie, string fase, string tipo, decimal kcalPorKg, string? observacoes,
        string? fotoUrl = null) =>
        Atualizar(nome, especie, fase, tipo, kcalPorKg, observacoes, fotoUrl);

    public void Atualizar(string nome, string especie, string fase, string tipo, decimal kcalPorKg, string? observacoes,
        string? fotoUrl = null)
    {
        Nome = Validar.Texto(nome, "nome", 80, "Informe o nome da ração.");
        Especie = Validar.Opcao(especie, Especies, "especie", "Escolha a espécie.");
        Fase = Validar.Opcao(fase, Fases, "fase", "Escolha a fase de vida.");
        Tipo = Validar.Opcao(tipo, Tipos, "tipo", "Escolha o tipo.");
        KcalPorKg = Validar.Faixa(kcalPorKg, 300m, 6000m, "kcalPorKg",
            "Informe a energia metabolizável entre 300 e 6.000 kcal/kg (está no rótulo).");
        Observacoes = Validar.TextoLivre(observacoes, "observacoes", 300);
        FotoUrl = Validar.TextoOpcional(fotoUrl, "fotoUrl", 500);
    }
}
