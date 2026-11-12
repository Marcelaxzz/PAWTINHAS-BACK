using System.Text;
using Microsoft.Extensions.Options;
using Pawtinhas.Application.Autenticacao;
using Pawtinhas.Application.Configuracao;
using Pawtinhas.Domain.Comum;
using Pawtinhas.Infrastructure;
using Pawtinhas.Infrastructure.Importacao;

namespace Pawtinhas.API.Cli;

/// <summary>
/// Comandos de manutenção: "dotnet run -- &lt;comando&gt;".
///   migrar                                     aplica as migrações do banco
///   usuario --nome "Nome"                      cria/atualiza a conta do e-mail permitido (pede a senha)
///   usuario --nome "Nome" --senha-stdin        lê a senha da entrada padrão (automação)
///   dados-exemplo [--substituir]               importa Seed/dados-exemplo.json
///   importar &lt;arquivo.json&gt; [--substituir]      importa dados de exemplo ou backup da versão anterior
///   importar &lt;arquivo.json&gt; --acrescentar       soma ao banco atual sem apagar (pula nomes repetidos)
/// A senha nunca vai na linha de comando: ficaria no histórico do terminal e na lista de processos.
/// </summary>
public static class Comandos
{
    private static readonly string[] Conhecidos = ["migrar", "usuario", "dados-exemplo", "importar"];

    public static bool Reconhece(string[] args) => args.Length > 0 && Conhecidos.Contains(args[0]);

    public static async Task<int> ExecutarAsync(IServiceProvider servicos, string[] args)
    {
        try
        {
            switch (args[0])
            {
                case "migrar":
                    await servicos.MigrarBancoAsync();
                    Console.WriteLine("Banco de dados atualizado.");
                    return 0;

                case "usuario":
                    return await UsuarioAsync(servicos, args);

                case "dados-exemplo":
                    return await ImportarAsync(servicos, Path.Combine(AppContext.BaseDirectory, "Seed", "dados-exemplo.json"), args);

                case "importar":
                    if (args.Length < 2 || args[1].StartsWith("--", StringComparison.Ordinal))
                    {
                        Console.Error.WriteLine("Informe o arquivo: dotnet run -- importar caminho/do/arquivo.json [--substituir]");
                        return 2;
                    }
                    return await ImportarAsync(servicos, args[1], args);
            }
        }
        catch (DominioException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (InvalidDataException ex)
        {
            Console.Error.WriteLine($"Dados inválidos: {ex.Message}");
            return 1;
        }

        return 2;
    }

    private static async Task<int> UsuarioAsync(IServiceProvider servicos, string[] args)
    {
        var nome = Valor(args, "--nome");
        if (string.IsNullOrWhiteSpace(nome))
        {
            Console.Error.WriteLine("Informe o nome: dotnet run -- usuario --nome \"Carolina Gonçalves\"");
            return 2;
        }

        await using var escopo = servicos.CreateAsyncScope();
        var email = escopo.ServiceProvider.GetRequiredService<IOptions<AutenticacaoOptions>>().Value.EmailPermitido;

        string senha;
        if (args.Contains("--senha-stdin"))
        {
            senha = (Console.In.ReadLine() ?? "").TrimEnd('\r', '\n');
        }
        else
        {
            Console.WriteLine($"Conta de acesso: {email}");
            senha = LerSenha("Senha: ");
            if (LerSenha("Repita a senha: ") != senha)
            {
                Console.Error.WriteLine("As senhas não conferem.");
                return 1;
            }
        }

        var (conta, nova) = await escopo.ServiceProvider.GetRequiredService<ServicoUsuario>().SalvarAsync(nome, senha);
        Console.WriteLine(nova ? $"Conta criada para {conta}." : $"Conta {conta} atualizada; as sessões abertas foram encerradas.");
        return 0;
    }

    private static async Task<int> ImportarAsync(IServiceProvider servicos, string arquivo, string[] args)
    {
        await using var escopo = servicos.CreateAsyncScope();
        return await escopo.ServiceProvider.GetRequiredService<ImportadorDados>()
            .ImportarAsync(arquivo, args.Contains("--substituir"), args.Contains("--acrescentar"));
    }

    private static string? Valor(string[] args, string nome)
    {
        var i = Array.IndexOf(args, nome);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    /// <summary>Lê a senha sem mostrá-la na tela.</summary>
    private static string LerSenha(string rotulo)
    {
        Console.Write(rotulo);
        var senha = new StringBuilder();
        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Enter) break;
            if (tecla.Key == ConsoleKey.Backspace)
            {
                if (senha.Length > 0) senha.Length--;
                continue;
            }
            if (!char.IsControl(tecla.KeyChar)) senha.Append(tecla.KeyChar);
        }
        Console.WriteLine();
        return senha.ToString();
    }
}
