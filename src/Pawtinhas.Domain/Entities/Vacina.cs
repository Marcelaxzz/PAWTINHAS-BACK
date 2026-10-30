namespace Pawtinhas.Domain;

public class Vacina : Evento
{
    public string NomeVacina { get; private set; } = string.Empty;
    public string Dose { get; private set; } = string.Empty;

    private Vacina() { }

    public Vacina(
        int animalId,
        DateTime dataHora,
        string nomeVacina,
        string dose,
        string? observacoes)
        : base(animalId, dataHora, observacoes)
    {
        NomeVacina = nomeVacina;
        Dose = dose;
    }
}