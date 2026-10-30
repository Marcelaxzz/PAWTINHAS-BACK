using Pawtinhas.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Pawtinhas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddTransient<AnimalAppService>();
        services.AddTransient<ResponsavelAppService>();
        services.AddScoped<EventoAppService>();

        return services;
    }
}