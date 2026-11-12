using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pawtinhas.Application.Abstracoes;
using Pawtinhas.Application.Configuracao;
using Pawtinhas.Application.Email;
using Pawtinhas.Application.Services;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Infrastructure.Rotinas;

/// <summary>Remove periodicamente códigos vencidos e sessões antigas.</summary>
public sealed class ServicoLimpeza(IServiceScopeFactory escopos, TimeProvider relogio, ILogger<ServicoLimpeza> log) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo, relogio);
        do
        {
            try
            {
                await using var escopo = escopos.CreateAsyncScope();
                var agora = relogio.GetUtcNow().UtcDateTime;
                var codigos = await escopo.ServiceProvider.GetRequiredService<IDesafioCodigoRepository>().LimparAsync(agora);
                var sessoes = await escopo.ServiceProvider.GetRequiredService<ISessaoRepository>().LimparAsync(agora);
                if (codigos + sessoes > 0) log.LogInformation("Limpeza: {Codigos} códigos e {Sessoes} sessões removidos.", codigos, sessoes);
            }
            // Se a API está fechando (porta ocupada, Ctrl+C), não registra: o log já pode ter sido descartado.
            catch (Exception ex) when (ex is not OperationCanceledException && !stoppingToken.IsCancellationRequested)
            {
                log.LogWarning(ex, "Falha na limpeza periódica.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

/// <summary>
/// Rotinas ligadas nas configurações da clínica: backup automático (a partir das 3h) e resumo da
/// agenda do dia por e-mail (a partir das 7h). Cada uma roda no máximo uma vez por dia; se o
/// servidor estava desligado no horário, roda assim que voltar.
/// </summary>
public sealed class ServicoRotinasDiarias(
    IServiceScopeFactory escopos,
    IServicoBackup backup,
    IFilaEmails emails,
    ModelosEmail modelos,
    IOptions<AutenticacaoOptions> autenticacao,
    TimeProvider relogio,
    ILogger<ServicoRotinasDiarias> log) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(5);
    private const int HoraBackup = 3;
    private const int HoraResumo = 7;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo, relogio);
        do
        {
            try
            {
                await ExecutarAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogWarning(ex, "Falha nas rotinas diárias.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExecutarAsync(CancellationToken ct)
    {
        await using var escopo = escopos.CreateAsyncScope();
        var servicos = escopo.ServiceProvider;
        var config = await servicos.GetRequiredService<IConfiguracaoRepository>().ObterAsync();
        var unitOfWork = servicos.GetRequiredService<IUnitOfWork>();

        var agora = relogio.GetLocalNow().DateTime;
        var hoje = DateOnly.FromDateTime(agora);

        if (config.BackupAutomatico && agora.Hour >= HoraBackup && !Hoje(config.UltimoBackup, hoje))
        {
            var arquivo = await backup.SalvarNaPastaAsync(ct);
            config.RegistrarBackup(agora);
            await unitOfWork.CommitAsync();
            log.LogInformation("Backup automático gravado em {Arquivo}", arquivo);
        }

        if (config.ResumoDiarioEmail && agora.Hour >= HoraResumo && !Hoje(config.UltimoResumo, hoje))
        {
            var email = Usuario.NormalizarEmail(autenticacao.Value.EmailPermitido);
            if (await servicos.GetRequiredService<IUsuarioRepository>().ObterPorEmailAsync(email) is not { } usuario) return;

            var itens = await servicos.GetRequiredService<AgendaAppService>().ItensDoDiaAsync(hoje);
            emails.Enfileirar(modelos.ResumoDiario(usuario.Email, usuario.Nome, hoje, itens));
            config.RegistrarResumo(agora);
            await unitOfWork.CommitAsync();
        }
    }

    private static bool Hoje(DateTime? quando, DateOnly hoje) => quando is { } q && DateOnly.FromDateTime(q) == hoje;
}
