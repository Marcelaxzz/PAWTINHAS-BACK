using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

public class Vacina : Evento
{
    private static readonly string[] SituacoesValidas = ["Agendado", "Pendente", "Aplicada", "Cancelado"];
    public static readonly string[] Doses = ["1ª Dose", "2ª Dose", "3ª Dose", "Reforço"];

    public string NomeVacina { get; private set; } = string.Empty;
    public string Dose { get; private set; } = string.Empty;

    /// <summary>Preenchido quando a aplicação é registrada.</summary>
    public DateTime? AplicadaEm { get; private set; }

    public override IReadOnlyList<string> StatusPermitidos => SituacoesValidas;

    private Vacina() { }

    public Vacina(
        int animalId,
        DateTime dataHora,
        string nomeVacina,
        string dose,
        string? observacoes)
        : base(animalId, dataHora, observacoes)
    {
        Atualizar(nomeVacina, dose);
    }

    public void Atualizar(string nomeVacina, string dose)
    {
        NomeVacina = Validar.Texto(nomeVacina, "nomeVacina", 60, "Informe o nome da vacina.");
        Dose = Validar.Opcao(dose, Doses, "dose", "Escolha a dose.");
    }

    public override void AlterarStatus(string status)
    {
        base.AlterarStatus(status);
        AplicadaEm = Status == "Aplicada" ? AplicadaEm ?? DateTime.Now : null;
    }
}
