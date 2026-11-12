using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;
using Pawtinhas.Application.Configuracao;

namespace Pawtinhas.API.Configuracao;

/// <summary>
/// Deixa o ESP32 achar a API sozinho em qualquer rede: a placa manda "PAWTINHAS?" por broadcast UDP
/// (porta Dispositivo:PortaDescoberta) e a API responde "PAWTINHAS http 5135". A placa usa o IP de quem
/// respondeu, então ninguém precisa escrever o IP do computador no código.
/// Fica desligado sem Dispositivo:Chave ou com a porta 0.
/// </summary>
public sealed partial class DescobertaDispositivo(
    IServer servidor,
    IOptions<DispositivoOptions> opcoes,
    IHostApplicationLifetime ciclo,
    ILogger<DescobertaDispositivo> log) : BackgroundService
{
    private const string Pergunta = "PAWTINHAS?";

    protected override async Task ExecuteAsync(CancellationToken parar)
    {
        var porta = opcoes.Value.PortaDescoberta;
        if (string.IsNullOrEmpty(opcoes.Value.Chave) || porta is <= 0 or > 65535) return;

        // A porta HTTP só é conhecida depois que o Kestrel começa a ouvir.
        var iniciou = new TaskCompletionSource();
        using (ciclo.ApplicationStarted.Register(() => iniciou.TrySetResult()))
        {
            try { await iniciou.Task.WaitAsync(parar); }
            catch (OperationCanceledException) { return; }
        }

        if (Endereco() is not var (esquema, portaHttp, soLocal))
        {
            log.LogWarning("Procura do ESP32 desligada: não achei o endereço HTTP da API.");
            return;
        }
        if (soLocal)
            log.LogWarning("A API só aceita conexões deste computador (localhost). O ESP32 e o celular não vão alcançá-la. " +
                           "Rode com o perfil padrão (http://*:5135) ou com --urls http://*:5135.");

        using var udp = new UdpClient(AddressFamily.InterNetwork);
        udp.EnableBroadcast = true;
        try
        {
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, porta));
        }
        catch (SocketException e)
        {
            log.LogWarning("Procura do ESP32 desligada: a porta UDP {Porta} está em uso ({Erro}).", porta, e.SocketErrorCode);
            return;
        }

        var resposta = Encoding.ASCII.GetBytes($"PAWTINHAS {esquema} {portaHttp}");
        log.LogInformation("ESP32 pode achar a API pela porta UDP {Porta} ({Esquema}, porta {PortaHttp}).", porta, esquema, portaHttp);

        while (!parar.IsCancellationRequested)
        {
            try
            {
                var msg = await udp.ReceiveAsync(parar);
                if (msg.Buffer.Length > 32 || Encoding.ASCII.GetString(msg.Buffer).Trim() != Pergunta) continue;
                await udp.SendAsync(resposta, msg.RemoteEndPoint, parar);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException)
            {
                // Pacote ruim ou destino que sumiu: segue ouvindo.
            }
        }
    }

    /// <summary>Endereço que a placa deve usar: prefere http (a placa não precisa de certificado na rede local).</summary>
    private (string Esquema, int Porta, bool SoLocal)? Endereco()
    {
        var enderecos = servidor.Features.Get<IServerAddressesFeature>()?.Addresses ?? [];
        var lidos = enderecos
            .Select(e => PadraoEndereco().Match(e))
            .Where(m => m.Success)
            .Select(m => (Esquema: m.Groups[1].Value, Host: m.Groups[2].Value, Porta: int.Parse(m.Groups[3].Value)))
            .OrderBy(e => e.Esquema == "http" ? 0 : 1)
            .ToList();
        if (lidos.Count == 0) return null;

        static bool Local(string host) => host is "localhost" or "127.0.0.1" or "[::1]";
        var escolhido = lidos.FirstOrDefault(e => !Local(e.Host));
        return escolhido.Esquema is null
            ? (lidos[0].Esquema, lidos[0].Porta, true)
            : (escolhido.Esquema, escolhido.Porta, false);
    }

    [GeneratedRegex(@"^(https?)://(\[[^\]]+\]|[^:/]+):(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex PadraoEndereco();
}
