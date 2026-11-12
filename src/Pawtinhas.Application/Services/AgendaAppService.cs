using Pawtinhas.Application.Email;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Services;

/// <summary>Compromissos de um dia em ordem de horário (resumo diário por e-mail).</summary>
public class AgendaAppService(IEventoRepository eventos)
{
    internal static readonly string[] Encerrados = ["Cancelado", "Concluído", "Concluída", "Aplicada", "Alta médica"];

    public async Task<IReadOnlyList<ItemAgenda>> ItensDoDiaAsync(DateOnly dia)
    {
        var lista = await eventos.ListarDoDiaAsync(dia);
        return lista
            .Where(e => !Encerrados.Contains(e.Status))
            .Select(e => new ItemAgenda(TimeOnly.FromDateTime(e.DataHora), Tipo(e), Pet(e), Detalhe(e)))
            .OrderBy(i => i.Hora)
            .ToList();
    }

    internal static string Tipo(Evento e) => e switch
    {
        Consulta => "Consulta",
        Vacina => "Vacina",
        Cirurgia => "Cirurgia",
        BanhoTosa => "Banho e tosa",
        PetSitter => "Visita de pet sitter",
        Emergencia => "Emergência",
        _ => "Compromisso",
    };

    internal static string Pet(Evento e) => e.Animal?.Nome ?? (e as Emergencia)?.DescricaoAnimal ?? "Animal não cadastrado";

    internal static string Detalhe(Evento e) => e switch
    {
        Consulta c => c.Motivo,
        Vacina v => $"{v.NomeVacina} · {v.Dose}",
        Cirurgia c => c.Procedimento,
        BanhoTosa b => b.TipoServico,
        PetSitter p => $"{p.Servico} · {p.Local}",
        Emergencia em => $"{em.Prioridade}: {em.Motivo}",
        _ => "",
    };
}
