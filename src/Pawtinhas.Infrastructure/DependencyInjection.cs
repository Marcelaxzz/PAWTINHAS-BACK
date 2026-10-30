
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pawtinhas.Domain.Interfaces;
using Pawtinhas.Infrastructure.Data;
using Pawtinhas.Infrastructure.Utils;
using Pawtinhas.Infrastructure.Repositories;

namespace Pawtinhas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string DefaultConnection não configurada.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(8, 0, 0))));
        services.AddScoped<IAnimalRepository, AnimalRepository>();
        services.AddScoped<IResponsavelRepository, ResponsavelRepository>();
        services.AddScoped<IEventoRepository, EventoRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}