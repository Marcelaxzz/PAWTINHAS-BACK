namespace Pawtinhas.Domain;

public class Consulta : Evento
{
    public string Motivo { get; private set; } = string.Empty;

    private Consulta() { }

    public Consulta(
        int animalId,
        DateTime dataHora,
        string motivo,
        string? observacoes)
        : base(animalId, dataHora, observacoes)
    {
        Motivo = motivo;
    }
}