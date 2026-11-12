using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Pawtinhas.Application.Comum;
using Pawtinhas.Application.Configuracao;
using Pawtinhas.Application.DTOs;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Services;

/// <summary>
/// Integração com o alarme ESP32. O dispositivo envia a chave no cabeçalho X-Device-Key.
/// Sem Dispositivo:Chave configurada, a integração fica desligada (404).
/// </summary>
public class DispositivoAppService(
    IAlarmeRepository alarmes,
    IConfiguracaoRepository configuracoes,
    IUnitOfWork unitOfWork,
    IOptions<DispositivoOptions> opcoes,
    TimeProvider relogio)
{
    public bool Ativo => !string.IsNullOrEmpty(opcoes.Value.Chave);

    /// <summary>Confere a chave em tempo constante (o tempo de resposta não revela quantos caracteres batem).</summary>
    public void Autorizar(string chaveEnviada)
    {
        if (!Ativo) throw new KeyNotFoundException("Integração com o dispositivo desativada.");

        var confere = chaveEnviada.Length > 0 && CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(chaveEnviada)),
            SHA256.HashData(Encoding.UTF8.GetBytes(opcoes.Value.Chave)));

        if (!confere) throw new AutenticacaoException(401, "dispositivo_nao_autorizado", "Chave do dispositivo inválida.");
    }

    public async Task<IReadOnlyList<AlarmeDispositivoDto>> AlarmesAsync()
    {
        // "Alarmes no ESP32" desligado nas configurações: o dispositivo fica sem lembretes.
        var config = await RegistrarContatoAsync();
        if (!config.AlarmesEsp32) return [];

        return (await alarmes.ListarAtivosAsync())
            .Select(a => new AlarmeDispositivoDto(a.Id, a.Nome, a.Hora.ToString("HH:mm"), a.Recorrencia,
                a.Inicio.ToString("yyyy-MM-dd"), a.Descricao, a.Animal?.Nome))
            .ToList();
    }

    public async Task RegistrarDisparoAsync(DisparoDispositivoDto dto)
    {
        await RegistrarContatoAsync();
        var alarme = await alarmes.ObterPorIdAsync(dto.AlarmeId) ?? throw new KeyNotFoundException("Alarme não encontrado.");
        await alarmes.AdicionarDisparoAsync(new DisparoAlarme(alarme, QuandoTocou(dto.Quando), dto.Resultado));
        await unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Sem internet o ESP32 guarda o disparo e manda depois, com a hora em que tocou.
    /// Hora ausente, no futuro ou de mais de uma semana atrás (relógio da placa errado) vira "agora".
    /// </summary>
    private DateTime QuandoTocou(DateTime? informado)
    {
        var agora = relogio.GetLocalNow().DateTime;
        return informado is { } quando && quando <= agora.AddMinutes(5) && quando >= agora.AddDays(-7) ? quando : agora;
    }

    private async Task<ConfiguracaoClinica> RegistrarContatoAsync()
    {
        var config = await configuracoes.ObterAsync();
        if (config.RegistrarContatoDispositivo(relogio.GetLocalNow().DateTime)) await unitOfWork.CommitAsync();
        return config;
    }
}
