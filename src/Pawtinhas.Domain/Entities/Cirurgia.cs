using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

public class Cirurgia : Evento
{
    private static readonly string[] SituacoesValidas = ["Agendado", "Em andamento", "Concluído", "Cancelado"];

    public string Procedimento { get; private set; } = string.Empty;

    public override IReadOnlyList<string> StatusPermitidos => SituacoesValidas;

    private Cirurgia() { }

    public Cirurgia(
        int animalId,
        DateTime dataHora,
        string procedimento,
        string? observacoes)
        : base(animalId, dataHora, observacoes)
    {
        Atualizar(procedimento);
    }

    public void Atualizar(string procedimento) =>
        Procedimento = Validar.Texto(procedimento, "procedimento", 120, "Informe o procedimento.");
}
