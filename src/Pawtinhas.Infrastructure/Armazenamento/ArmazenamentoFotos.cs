using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pawtinhas.Application.Abstracoes;
using Pawtinhas.Domain.Comum;
using Pawtinhas.Infrastructure.Configuracao;
using Pawtinhas.Infrastructure.Data;

namespace Pawtinhas.Infrastructure.Armazenamento;

/// <summary>
/// Fotos com nome aleatório de 128 bits. Ficam no banco (tabela Arquivos, permanente) e numa pasta
/// em disco (Armazenamento:Pasta) que serve de cópia rápida: se a pasta sumir, como no Render
/// gratuito a cada reinício, o arquivo é regravado a partir do banco no primeiro pedido.
/// </summary>
public sealed partial class ArmazenamentoFotos : IArmazenamentoFotos
{
    public const string PrefixoUrl = "/api/arquivos/";

    private readonly string _pasta;
    private readonly long _limiteBytes;
    private readonly ILogger<ArmazenamentoFotos> _log;
    private readonly IServiceScopeFactory _escopos;
    private readonly TimeProvider _relogio;

    public ArmazenamentoFotos(
        IOptions<ArmazenamentoOptions> opcoes,
        IHostEnvironment env,
        ILogger<ArmazenamentoFotos> log,
        IServiceScopeFactory escopos,
        TimeProvider relogio)
    {
        _pasta = Path.GetFullPath(Path.Combine(env.ContentRootPath, opcoes.Value.Pasta));
        _limiteBytes = opcoes.Value.TamanhoMaximoMb * 1024L * 1024L;
        _log = log;
        _escopos = escopos;
        _relogio = relogio;
        Directory.CreateDirectory(_pasta);
    }

    [GeneratedRegex(@"^[a-f0-9]{32}\.(jpg|png|webp)$")]
    private static partial Regex NomeValido();

    public async Task<string> SalvarAsync(Stream conteudo, CancellationToken ct)
    {
        // Lê no máximo o limite + 1 byte: arquivo maior é recusado sem ocupar memória à toa.
        using var memoria = new MemoryStream();
        var buffer = new byte[81920];
        int lidos;
        while ((lidos = await conteudo.ReadAsync(buffer, ct)) > 0)
        {
            memoria.Write(buffer, 0, lidos);
            if (memoria.Length > _limiteBytes)
                throw new DominioException("arquivo", $"A imagem deve ter até {_limiteBytes / 1024 / 1024} MB.");
        }

        var bytes = memoria.ToArray();
        var extensao = DetectarExtensao(bytes) ?? throw new DominioException("arquivo", "Envie uma imagem PNG, JPG ou WebP.");
        var nome = $"{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16))}.{extensao}";

        await using (var escopo = _escopos.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Arquivos.Add(new ArquivoArmazenado(nome, TipoConteudo(nome), bytes, _relogio.GetUtcNow().UtcDateTime));
            await db.SaveChangesAsync(ct);
        }
        await File.WriteAllBytesAsync(Path.Combine(_pasta, nome), bytes, ct);
        return PrefixoUrl + nome;
    }

    public string? ValidarUrl(string? url, string campo)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        url = url.Trim();

        if (url.StartsWith(PrefixoUrl, StringComparison.Ordinal))
        {
            return Localizar(url[PrefixoUrl.Length..]) is not null
                ? url
                : throw new DominioException(campo, "Foto não encontrada. Envie a imagem novamente.");
        }

        if (url.Length <= 500 && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo))
        {
            return uri.AbsoluteUri;
        }

        throw new DominioException(campo, "Use uma foto enviada pelo sistema ou um link https.");
    }

    public void RemoverSeLocal(string? url)
    {
        if (url is null || !url.StartsWith(PrefixoUrl, StringComparison.Ordinal)) return;
        var nome = url[PrefixoUrl.Length..];
        if (CaminhoFisico(nome) is not { } caminho) return;
        try
        {
            using var escopo = _escopos.CreateScope();
            escopo.ServiceProvider.GetRequiredService<AppDbContext>().Arquivos
                .Where(a => a.Nome == nome).ExecuteDelete();
            File.Delete(caminho);
        }
        catch (IOException ex)
        {
            _log.LogWarning(ex, "Não foi possível remover o arquivo {Arquivo}", caminho);
        }
    }

    public ArquivoFoto? Localizar(string nome)
    {
        if (CaminhoFisico(nome) is not { } caminho) return null;
        if (File.Exists(caminho)) return new ArquivoFoto(caminho, TipoConteudo(nome));

        // Fora do disco (pasta apagada no reinício): busca no banco e regrava a cópia local.
        using var escopo = _escopos.CreateScope();
        var arquivo = escopo.ServiceProvider.GetRequiredService<AppDbContext>().Arquivos
            .AsNoTracking().FirstOrDefault(a => a.Nome == nome);
        if (arquivo is null) return null;
        File.WriteAllBytes(caminho, arquivo.Dados);
        return new ArquivoFoto(caminho, arquivo.TipoConteudo);
    }

    /// <summary>Caminho físico de um nome de arquivo. Só aceita nomes gerados por nós (sem "../").</summary>
    private string? CaminhoFisico(string nome) => NomeValido().IsMatch(nome) ? Path.Combine(_pasta, nome) : null;

    /// <summary>Identifica a imagem pela assinatura binária (não confia na extensão nem no tipo informado).</summary>
    internal static string? DetectarExtensao(ReadOnlySpan<byte> b)
    {
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "png";
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "jpg";
        if (b.Length >= 12 && b[..4].SequenceEqual("RIFF"u8) && b[8..12].SequenceEqual("WEBP"u8)) return "webp";
        return null;
    }

    private static string TipoConteudo(string nome) => Path.GetExtension(nome) switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "image/jpeg",
    };
}
