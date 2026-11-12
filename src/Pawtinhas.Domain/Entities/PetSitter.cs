using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>Visitas domiciliares diárias de DataHora até DataHoraFim, sempre no mesmo horário.</summary>
public class PetSitter : Evento
{
    private static readonly string[] SituacoesValidas = ["Pendente", "Concluída", "Cancelado"];
    public static readonly string[] Periodos = ["Manhã", "Tarde", "Noite", "Integral"];

    public DateTime DataHoraFim { get; private set; }
    public string Local { get; private set; } = string.Empty;
    public string Servico { get; private set; } = string.Empty;
    public string Periodo { get; private set; } = string.Empty;

    public override IReadOnlyList<string> StatusPermitidos => SituacoesValidas;

    private PetSitter() { }

    public PetSitter(
        int animalId,
        DateTime dataHora,
        DateTime dataHoraFim,
        string local,
        string? observacoes,
        string servico = "Visita",
        string periodo = "Integral")
        : base(animalId, dataHora, observacoes)
    {
        Atualizar(dataHora, dataHoraFim, local, servico, periodo);
    }

    public void Atualizar(DateTime inicio, DateTime fim, string local, string servico, string periodo)
    {
        if (fim.Date < inicio.Date)
            throw new DominioException("dataHoraFim", "O término deve ser no mesmo dia ou depois do início.");

        DataHoraFim = fim;
        Local = Validar.Texto(local, "local", 160, "Informe o endereço da visita.");
        Servico = Validar.Texto(servico, "servico", 80, "Escolha o serviço.");
        Periodo = Validar.Opcao(periodo, Periodos, "periodo", "Escolha o período.");
    }
}
