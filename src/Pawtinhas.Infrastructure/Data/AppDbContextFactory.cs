
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Pawtinhas.Infrastructure.Data;

/// <summary>
/// Usada só pelo "dotnet ef migrations add". Gerar migração não conecta no banco, então a
/// conexão abaixo não tem senha nem servidor real (nada de credencial no código).
/// Para aplicar migrações use "dotnet run -- migrar", que lê a conexão da configuração.
/// </summary>
public class AppDbContextFactory
    : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseMySql(
            "Server=localhost;Database=pawtinhas_design",
            DependencyInjection.VersaoMySql);

        return new AppDbContext(optionsBuilder.Options);
    }
}
