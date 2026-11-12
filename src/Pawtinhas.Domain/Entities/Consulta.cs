using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

public class Consulta : Evento
{
    private static readonly string[] SituacoesValidas = ["Agendado", "Em andamento", "Concluído", "Cancelado"];

    public string Motivo { get; private set; } = string.Empty;

    public override IReadOnlyList<string> StatusPermitidos => SituacoesValidas;

    private Consulta() { }

    public Consulta(
        int animalId,
        DateTime dataHora,
        string motivo,
        string? observacoes)
        : base(animalId, dataHora, observacoes)
    {
        Atualizar(motivo);
    }

    public void Atualizar(string motivo) =>
        Motivo = Validar.Texto(motivo, "motivo", 120, "Informe o motivo da consulta.");
}
