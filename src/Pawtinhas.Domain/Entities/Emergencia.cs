namespace Pawtinhas.Domain;

public class Emergencia : Evento
{
    public string Motivo { get; private set; } = string.Empty;
    public string Prioridade { get; private set; } = string.Empty;

    private Emergencia() { }

    public Emergencia(
        int animalId,
        DateTime dataHora,
        string motivo,
        string prioridade,
        string? observacoes)
        : base(animalId, dataHora, observacoes)
    {
        Motivo = motivo;
        Prioridade = prioridade;
    }
}