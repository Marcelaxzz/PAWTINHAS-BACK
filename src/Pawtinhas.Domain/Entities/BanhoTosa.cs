namespace Pawtinhas.Domain;

public class BanhoTosa : Evento
{
    public string TipoServico { get; private set; } = string.Empty;

    private BanhoTosa() { }

    public BanhoTosa(
        int animalId,
        DateTime dataHora,
        string tipoServico,
        string? observacoes)
        : base(animalId, dataHora, observacoes)
    {
        TipoServico = tipoServico;
    }
}