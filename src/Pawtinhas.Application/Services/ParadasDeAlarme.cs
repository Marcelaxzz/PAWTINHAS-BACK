using System.Collections.Concurrent;
using Pawtinhas.Application.DTOs;

namespace Pawtinhas.Application.Services;

/// <summary>
/// Quem parou cada toque de alarme. O painel, o app e o ESP32 tocam o mesmo alarme no mesmo minuto;
/// quando um deles para, avisa aqui, e os outros (que consultam enquanto tocam) param também.
///
/// Fica só na memória: um toque dura no máximo alguns minutos, então não há o que guardar no banco
/// (o histórico continua nos disparos que o ESP32 registra).
/// O minuto é o número de minutos desde 1970 (UTC), o mesmo valor no navegador, no celular e na placa.
/// </summary>
public sealed class ParadasDeAlarme(TimeProvider relogio)
{
    /// <summary>Por quanto tempo uma parada vale (o app toca por até 5 minutos).</summary>
    private const int GuardarPorMinutos = 15;

    private static readonly HashSet<string> Origens = ["painel", "app", "caixinha"];

    private readonly ConcurrentDictionary<(int AlarmeId, long Minuto), ParadaAlarmeDto> paradas = new();

    private long MinutoAtual => relogio.GetUtcNow().ToUnixTimeSeconds() / 60;

    /// <summary>
    /// Registra a parada dos alarmes daquele minuto. Minutos fora da janela (relógio do aparelho muito errado)
    /// são ignorados. Devolve quantos alarmes foram marcados.
    /// </summary>
    public int Registrar(PararAlarmeDto dto, string origemPadrao)
    {
        var agora = MinutoAtual;
        if (Math.Abs(agora - dto.Minuto) > GuardarPorMinutos) return 0;

        var origem = dto.Origem is { } o && Origens.Contains(o) ? o : origemPadrao;
        var em = relogio.GetUtcNow();
        var marcados = 0;
        foreach (var id in dto.AlarmeIds.Where(id => id > 0).Distinct())
        {
            // A primeira parada vale: se dois aparelhos param juntos, fica quem parou primeiro.
            if (paradas.TryAdd((id, dto.Minuto), new ParadaAlarmeDto(id, dto.Minuto, origem, em))) marcados++;
        }
        Limpar(agora);
        return marcados;
    }

    /// <summary>Paradas dos últimos minutos, para quem ainda está tocando conferir.</summary>
    public IReadOnlyList<ParadaAlarmeDto> Recentes()
    {
        Limpar(MinutoAtual);
        return paradas.Values.OrderBy(p => p.Em).ToList();
    }

    private void Limpar(long agora)
    {
        foreach (var chave in paradas.Keys)
        {
            if (agora - chave.Minuto > GuardarPorMinutos) paradas.TryRemove(chave, out _);
        }
    }
}
