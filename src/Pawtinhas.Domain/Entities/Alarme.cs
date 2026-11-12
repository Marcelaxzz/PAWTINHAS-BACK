using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>
/// Lembrete que toca no ESP32, no app do celular e no painel aberto no navegador.
/// <see cref="Inicio"/> é o dia a partir do qual ele vale: "Semanal" toca no dia da semana do início
/// e "Somente hoje" toca só nesse dia. Assim todos os aparelhos tocam no mesmo dia.
/// </summary>
public class Alarme
{
    public const string SomenteHoje = "Somente hoje";

    public static readonly string[] Recorrencias =
        ["Diário (Todos os dias)", "A cada 8 horas", "A cada 12 horas", "Dias úteis", "Semanal", SomenteHoje];

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public int? AnimalId { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public TimeOnly Hora { get; private set; }
    public string Recorrencia { get; private set; } = string.Empty;
    public DateOnly Inicio { get; private set; }
    public bool Ativo { get; private set; }

    public Animal? Animal { get; private set; }

    private Alarme() { }

    public Alarme(string nome, int? animalId, string? descricao, TimeOnly hora, string recorrencia, DateOnly inicio, bool ativo) =>
        Atualizar(nome, animalId, descricao, hora, recorrencia, inicio, ativo);

    public void Atualizar(string nome, int? animalId, string? descricao, TimeOnly hora, string recorrencia, DateOnly inicio, bool ativo)
    {
        Nome = Validar.Texto(nome, "nome", 60, "Dê um nome ao alarme.");
        AnimalId = animalId;
        Descricao = Validar.TextoLivre(descricao, "descricao", 140);
        Hora = hora;
        Recorrencia = Validar.Opcao(recorrencia, Recorrencias, "recorrencia", "Escolha a recorrência.");
        if (inicio.Year < 2000 || inicio.Year > 2100) throw new DominioException("inicio", "Informe uma data válida.");
        Inicio = inicio;
        Ativo = ativo;
    }

    public void DefinirAtivo(bool ativo) => Ativo = ativo;
}
