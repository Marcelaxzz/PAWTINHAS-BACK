namespace Pawtinhas.Domain;

public class Cirurgia : Evento
{
    public string Procedimento { get; private set; } = string.Empty;

    private Cirurgia() { }

    public Cirurgia(
        int animalId,
        DateTime dataHora,
        string procedimento,
        string? observacoes)
        : base(animalId, dataHora, observacoes)
    {
        Procedimento = procedimento;
    }
}