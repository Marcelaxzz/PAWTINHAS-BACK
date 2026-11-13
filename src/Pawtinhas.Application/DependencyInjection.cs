using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pawtinhas.Application.Autenticacao;
using Pawtinhas.Application.Configuracao;
using Pawtinhas.Application.Email;
using Pawtinhas.Application.Services;

namespace Pawtinhas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Configurações validadas na inicialização: a API não sobe com valores faltando.
        services.AddOptions<AutenticacaoOptions>()
            .Bind(configuration.GetSection(AutenticacaoOptions.Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<DispositivoOptions>().Bind(configuration.GetSection(DispositivoOptions.Secao));
        services.AddOptions<FrontendOptions>().Bind(configuration.GetSection(FrontendOptions.Secao));

        services.AddScoped<AnimalAppService>();
        services.AddScoped<ResponsavelAppService>();
        services.AddScoped<EventoAppService>();
        services.AddScoped<AdocaoAppService>();
        services.AddScoped<ProfissionalAppService>();
        services.AddScoped<TutorAppService>(); 
        services.AddScoped<MedicamentoAppService>();
        services.AddScoped<RacaoAppService>();
        services.AddScoped<MaterialAppService>();
        services.AddScoped<AlarmeAppService>();
        services.AddScoped<MedicaoAppService>();
        services.AddScoped<DispositivoAppService>();
        services.AddScoped<ConfiguracaoAppService>();
        services.AddScoped<ResolvedorCalculos>();
        services.AddScoped<CalculoAppService>();
        services.AddScoped<PrescricaoAppService>();
        services.AddScoped<PlanoAlimentarAppService>();
        services.AddScoped<AgendaAppService>();
        services.AddScoped<EstoqueAppService>();
        services.AddScoped<RelatorioServicoAppService>();
        services.AddScoped<RelatorioAppService>();

        services.AddScoped<ServicoAutenticacao>();
        services.AddScoped<ServicoSessoes>();
        services.AddScoped<ServicoUsuario>();
        services.AddScoped<ServicoContas>();
        services.AddSingleton<ModelosEmail>();

        return services;
    }
}
