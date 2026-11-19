
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pawtinhas.Application.Abstracoes;
using Pawtinhas.Application.Relatorios;
using Pawtinhas.Domain.Interfaces;
using Pawtinhas.Infrastructure.Armazenamento;
using Pawtinhas.Infrastructure.Configuracao;
using Pawtinhas.Infrastructure.Data;
using Pawtinhas.Infrastructure.Email;
using Pawtinhas.Infrastructure.Importacao;
using Pawtinhas.Infrastructure.Relatorios;
using Pawtinhas.Infrastructure.Repositories;
using Pawtinhas.Infrastructure.Rotinas;
using Pawtinhas.Infrastructure.Seguranca;
using Pawtinhas.Infrastructure.Utils;

namespace Pawtinhas.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Versão mínima do MySQL usada pelo provedor (a mesma da equipe).</summary>
    public static readonly MySqlServerVersion VersaoMySql = new(new Version(8, 0, 0));

    /// <summary>
    /// Banco, repositórios, segurança, e-mail e armazenamento. As rotinas em segundo plano
    /// só sobem com o servidor (não durante os comandos de manutenção).
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env,
        bool executandoComando = false)
    {
        // ---------- Configurações (validadas na inicialização) ----------
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.Secao)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<ArmazenamentoOptions>().Bind(configuration.GetSection(ArmazenamentoOptions.Secao)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.Secao))
            .ValidateDataAnnotations()
            .Validate(e => e.Modo != ModoEmail.Smtp || !string.IsNullOrWhiteSpace(e.Host),
                "Configure Email:Host (servidor SMTP) para enviar os códigos de acesso.")
            .Validate(e => e.Modo != ModoEmail.AppsScript
                    || (e.UrlAppsScript.StartsWith("https://script.google.com/", StringComparison.Ordinal) && e.TokenAppsScript.Length >= 32),
                "Configure Email:UrlAppsScript (https://script.google.com/...) e Email:TokenAppsScript (32+ caracteres).")
            .Validate(e => e.Modo != ModoEmail.Pasta || env.IsDevelopment(),
                "Email:Modo = Pasta só é permitido em desenvolvimento.")
            .ValidateOnStart();

        // ---------- Banco de dados ----------
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Connection string DefaultConnection não configurada (veja o README: user-secrets ou .env).");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, VersaoMySql));

        services.AddScoped(typeof(IRepository<>), typeof(RepositoryBase<>));
        services.AddScoped<IAnimalRepository, AnimalRepository>();
        services.AddScoped<IResponsavelRepository, ResponsavelRepository>();
        services.AddScoped<IEventoRepository, EventoRepository>();
        services.AddScoped<IAlarmeRepository, AlarmeRepository>();
        services.AddScoped<IPrescricaoRepository, PrescricaoRepository>();
        services.AddScoped<IPlanoAlimentarRepository, PlanoAlimentarRepository>();
        services.AddScoped<IConfiguracaoRepository, ConfiguracaoRepository>();
        services.AddScoped<IEstoqueRepository, EstoqueRepository>();
        services.AddScoped<IRelatorioServicoRepository, RelatorioServicoRepository>();
        services.AddSingleton<IGeradorPdf, GeradorPdf>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IDesafioCodigoRepository, DesafioCodigoRepository>();
        services.AddScoped<ISessaoRepository, SessaoRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ImportadorDados>();

        // ---------- Serviços ----------
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IHashSenha, HashSenha>();
        services.AddSingleton<IServicoCodigos, ServicoCodigos>();
        services.AddSingleton<IGeradorTokenAcesso, GeradorTokenAcesso>();
        services.AddSingleton<IArmazenamentoFotos, ArmazenamentoFotos>();
        services.AddSingleton<IServicoBackup, ServicoBackup>();
        services.AddSingleton<IEstadoEmail, EstadoEmail>();
        services.AddSingleton<FilaEmails>();
        services.AddSingleton<IFilaEmails>(sp => sp.GetRequiredService<FilaEmails>());
        services.AddSingleton<IEnviadorEmail>(sp => sp.GetRequiredService<IOptions<EmailOptions>>().Value.Modo switch
        {
            ModoEmail.Pasta => ActivatorUtilities.CreateInstance<EnviadorPasta>(sp),
            ModoEmail.AppsScript => ActivatorUtilities.CreateInstance<EnviadorAppsScript>(sp),
            _ => ActivatorUtilities.CreateInstance<EnviadorSmtp>(sp),
        });

        if (!executandoComando)
        {
            services.AddHostedService<ServicoEnvioEmails>();
            services.AddHostedService<ServicoLimpeza>();
            services.AddHostedService<ServicoRotinasDiarias>();
        }

        return services;
    }

    /// <summary>Aplica as migrações pendentes (comando "migrar" ou BancoDeDados:MigrarAoIniciar).</summary>
    public static async Task MigrarBancoAsync(this IServiceProvider servicos)
    {
        await using var escopo = servicos.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }
}
