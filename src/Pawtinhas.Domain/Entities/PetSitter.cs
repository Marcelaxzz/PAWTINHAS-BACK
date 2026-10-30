namespace Pawtinhas.Domain;

public class PetSitter : Evento
{
    public DateTime DataHoraFim { get; private set; }
    public string Local { get; private set; } = string.Empty;

    private PetSitter() { }

    public PetSitter(
        int animalId,
        DateTime dataHora,
        DateTime dataHoraFim,
        string local,
        string? observacoes)
        : base(animalId, dataHora, observacoes)
    {
        DataHoraFim = dataHoraFim;
        Local = local;
    }
}