namespace Pawtinhas.Domain;

public abstract class Evento
{
    public int Id { get; private set; }
    public int AnimalId { get; private set; }
    public DateTime DataHora { get; private set; }
    public string? Observacoes { get; private set; }
    public string Status { get; private set; } = "Agendado";

    public Animal Animal { get; private set; } = null!;

    protected Evento() { }

    protected Evento(int animalId, DateTime dataHora, string? observacoes)
    {
        AnimalId = animalId;
        DataHora = dataHora;
        Observacoes = observacoes;
    }

    public void AtualizarDados(
        DateTime dataHora,
        string? observacoes,
        string status)
    {
        DataHora = dataHora;
        Observacoes = observacoes;
        Status = status;
    }
}