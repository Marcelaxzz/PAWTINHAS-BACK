using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pawtinhas.Application.Abstracoes;
using Pawtinhas.Infrastructure.Configuracao;
using Pawtinhas.Infrastructure.Data;

namespace Pawtinhas.Infrastructure.Armazenamento;

/// <summary>
/// Cópia dos dados da clínica em JSON (download nas configurações e backup diário na pasta).
/// As fotos ficam na pasta de arquivos; o backup guarda só o caminho delas.
/// Dados de acesso (senha, códigos e sessões) nunca entram no arquivo.
/// </summary>
public sealed class ServicoBackup(IServiceScopeFactory escopos, IOptions<ArmazenamentoOptions> opcoes, IHostEnvironment env) : IServicoBackup
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _pasta = Path.GetFullPath(Path.Combine(env.ContentRootPath, opcoes.Value.PastaBackups));

    public async Task<byte[]> GerarAsync(CancellationToken ct)
    {
        await using var escopo = escopos.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();

        var dados = new Dictionary<string, object?>
        {
            ["geradoEm"] = DateTime.Now,
            ["responsaveis"] = await db.Responsaveis.AsNoTracking().ToListAsync(ct),
            ["profissionais"] = await db.Profissionais.AsNoTracking().ToListAsync(ct),
            ["animais"] = await db.Animais.AsNoTracking().Include(a => a.PerfilAdocao).ToListAsync(ct),
            // Como object, cada compromisso sai com os campos do próprio tipo (motivo, dose, local...).
            ["eventos"] = (await db.Eventos.AsNoTracking().ToListAsync(ct))
                .Select(e => new { tipo = e.GetType().Name, dados = (object)e }).ToList(),
            ["alarmes"] = await db.Alarmes.AsNoTracking().ToListAsync(ct),
            ["disparosAlarme"] = await db.DisparosAlarme.AsNoTracking().ToListAsync(ct),
            ["medicamentos"] = await db.Medicamentos.AsNoTracking().ToListAsync(ct),
            ["racoes"] = await db.Racoes.AsNoTracking().ToListAsync(ct),
            ["materiais"] = await db.Materiais.AsNoTracking().ToListAsync(ct),
            ["prescricoes"] = await db.Prescricoes.AsNoTracking().ToListAsync(ct),
            ["planosAlimentares"] = await db.PlanosAlimentares.AsNoTracking().ToListAsync(ct),
            ["configuracoes"] = await db.Configuracoes.AsNoTracking().ToListAsync(ct),
            ["estoque"] = await db.ItensEstoque.AsNoTracking().Include(i => i.Lotes).ToListAsync(ct),
            ["movimentacoesEstoque"] = await db.MovimentacoesEstoque.AsNoTracking().ToListAsync(ct),
            ["relatoriosServico"] = await db.RelatoriosServico.AsNoTracking().ToListAsync(ct),
        };

        return JsonSerializer.SerializeToUtf8Bytes(dados, Json);
    }

    public async Task<string> SalvarNaPastaAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(_pasta);
        var arquivo = Path.Combine(_pasta, $"pawtinhas-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        await File.WriteAllBytesAsync(arquivo, await GerarAsync(ct), ct);

        foreach (var antigo in Listar().Skip(opcoes.Value.BackupsMantidos)) File.Delete(antigo);
        return arquivo;
    }

    public IReadOnlyList<string> Listar() =>
        Directory.Exists(_pasta)
            ? Directory.GetFiles(_pasta, "pawtinhas-*.json").OrderDescending().ToList()
            : [];
}
