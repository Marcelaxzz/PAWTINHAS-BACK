using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>
/// Compromisso da agenda da clínica (herança por tabela única: consulta, vacina, cirurgia,
/// banho e tosa, pet sitter e emergência). Datas e horas são locais da clínica.
/// </summary>
public abstract class Evento
{
    public int Id { get; private set; }

    /// <summary>Obrigatório em todos os tipos, menos na emergência de um animal ainda sem cadastro.</summary>
    public int? AnimalId { get; private set; }
    public DateTime DataHora { get; private set; }
    public string? Observacoes { get; private set; }
    public string Status { get; private set; } = "Agendado";

    /// <summary>Profissional da equipe responsável (veterinário, groomer, cuidador...).</summary>
    public int? ProfissionalId { get; private set; }

    public DateTime CriadoEm { get; private set; } = DateTime.UtcNow;

    public Animal? Animal { get; private set; }
    public Profissional? Profissional { get; private set; }

    /// <summary>Status aceitos por este tipo de compromisso; o primeiro é o inicial.</summary>
    public abstract IReadOnlyList<string> StatusPermitidos { get; }

    protected Evento() { }

    protected Evento(int? animalId, DateTime dataHora, string? observacoes)
    {
        AnimalId = animalId;
        DataHora = dataHora;
        Observacoes = Validar.TextoOpcional(observacoes, "observacoes", 1000);
        Status = StatusPermitidos[0];
    }

    public void Reagendar(int? animalId, DateTime dataHora, int? profissionalId, string? observacoes)
    {
        AnimalId = animalId;
        DataHora = dataHora;
        ProfissionalId = profissionalId;
        Observacoes = Validar.TextoOpcional(observacoes, "observacoes", 1000);
    }

    public void DefinirProfissional(int? profissionalId) => ProfissionalId = profissionalId;

    public virtual void AlterarStatus(string status) =>
        Status = Validar.Opcao(status, StatusPermitidos, "status", $"Status inválido. Use: {string.Join(", ", StatusPermitidos)}.");

    /// <summary>Mantém o método da equipe: atualiza data, observações e status de uma vez.</summary>
    public void AtualizarDados(DateTime dataHora, string? observacoes, string status)
    {
        Reagendar(AnimalId, dataHora, ProfissionalId, observacoes);
        AlterarStatus(status);
    }
}
