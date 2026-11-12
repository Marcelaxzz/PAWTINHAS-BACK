using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

public class BanhoTosa : Evento
{
    private static readonly string[] SituacoesValidas = ["Agendado", "Em andamento", "Concluído", "Cancelado"];

    public string TipoServico { get; private set; } = string.Empty;

    public override IReadOnlyList<string> StatusPermitidos => SituacoesValidas;

    private BanhoTosa() { }

    public BanhoTosa(
        int animalId,
        DateTime dataHora,
        string tipoServico,
        string? observacoes)
        : base(animalId, dataHora, observacoes)
    {
        Atualizar(tipoServico);
    }

    public void Atualizar(string tipoServico) =>
        TipoServico = Validar.Texto(tipoServico, "tipoServico", 80, "Escolha o serviço.");
}
