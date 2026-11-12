using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>Atendimento de emergência. O animal pode chegar sem cadastro: aí vale a descrição.</summary>
public class Emergencia : Evento
{
    private static readonly string[] SituacoesValidas = ["Em atendimento", "Estabilizado", "Alta médica"];
    public static readonly string[] Prioridades = ["Baixa", "Média", "Alta", "Crítica"];

    public string Motivo { get; private set; } = string.Empty;
    public string Prioridade { get; private set; } = string.Empty;

    /// <summary>Identificação do animal não cadastrado (ex.: "cão sem raça definida, pelagem preta").</summary>
    public string? DescricaoAnimal { get; private set; }

    public override IReadOnlyList<string> StatusPermitidos => SituacoesValidas;

    private Emergencia() { }

    public Emergencia(
        int? animalId,
        DateTime dataHora,
        string motivo,
        string prioridade,
        string? observacoes,
        string? descricaoAnimal = null)
        : base(animalId, dataHora, observacoes)
    {
        Atualizar(animalId, motivo, prioridade, descricaoAnimal);
    }

    public void Atualizar(int? animalId, string motivo, string prioridade, string? descricaoAnimal)
    {
        Motivo = Validar.Texto(motivo, "motivo", 200, "Descreva o motivo da emergência.");
        Prioridade = Validar.Opcao(prioridade, Prioridades, "prioridade", "Escolha a prioridade.");
        DescricaoAnimal = animalId is null
            ? Validar.Texto(descricaoAnimal, "descricaoAnimal", 120, "Identifique o animal não cadastrado.")
            : null;
    }
}
