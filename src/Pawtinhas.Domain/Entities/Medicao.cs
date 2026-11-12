using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>Medição de um membro para a prótese 3D (centímetros). Os valores são médias de leituras.</summary>
public class Medicao
{
    public static readonly string[] Membros = ["Anterior esquerdo", "Anterior direito", "Posterior esquerdo", "Posterior direito"];
    public static readonly string[] Metodos = ["Câmera do celular", "Fita métrica", "Foto com cartão", "Foto com régua"];

    public int Id { get; private set; }
    public int AnimalId { get; private set; }
    public string? Membro { get; private set; }
    public decimal ComprimentoMembro { get; private set; }
    public decimal AlturaChao { get; private set; }
    public decimal LarguraPata { get; private set; }

    /// <summary>Largura do membro no ponto de encaixe da prótese (diâmetro interno do soquete).</summary>
    public decimal? LarguraEncaixe { get; private set; }

    public string Metodo { get; private set; } = "Fita métrica";
    public int Leituras { get; private set; } = 1;
    public DateTime CriadoEm { get; private set; }

    public Animal Animal { get; private set; } = null!;

    private Medicao() { }

    public Medicao(
        int animalId,
        string? membro,
        decimal comprimentoMembro,
        decimal alturaChao,
        decimal larguraPata,
        decimal? larguraEncaixe,
        string? metodo,
        int leituras,
        DateTime criadoEm)
    {
        AnimalId = animalId;
        Membro = string.IsNullOrWhiteSpace(membro) ? null : Validar.Opcao(membro, Membros, "membro", "Escolha o membro medido.");
        ComprimentoMembro = Medida(comprimentoMembro, "comprimentoMembro");
        AlturaChao = Medida(alturaChao, "alturaChao");
        LarguraPata = Medida(larguraPata, "larguraPata");
        LarguraEncaixe = larguraEncaixe is { } encaixe ? Medida(encaixe, "larguraEncaixe") : null;
        Metodo = string.IsNullOrWhiteSpace(metodo) ? "Fita métrica" : Validar.Opcao(metodo, Metodos, "metodo", "Método de medição inválido.");
        Leituras = Validar.Faixa(leituras, 1, 20, "leituras", "Use de 1 a 20 leituras.");
        CriadoEm = criadoEm;
    }

    private static decimal Medida(decimal valor, string campo) =>
        Math.Round(Validar.Faixa(valor, 0.1m, 200m, campo, "Informe uma medida entre 0,1 e 200 cm."), 2);
}
