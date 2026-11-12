using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pawtinhas.API.Http;
using Pawtinhas.Application.Abstracoes;

namespace Pawtinhas.API.Controllers;

/// <summary>
/// Fotos dos animais. O envio exige sessão; a leitura é pública porque a vitrine de adoção mostra as fotos.
/// Os nomes são aleatórios (128 bits), então não dá para listar nem adivinhar arquivos.
/// </summary>
[ApiController]
[Route("api/arquivos")]
public sealed class ArquivosController(IArmazenamentoFotos fotos) : ControllerBase
{
    /// <summary>Recebe uma imagem (multipart, campo "arquivo") e devolve a URL para usar em "fotoUrl".</summary>
    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Enviar(IFormFile arquivo, CancellationToken ct)
    {
        await using var conteudo = arquivo.OpenReadStream();
        var url = await fotos.SalvarAsync(conteudo, ct);
        return StatusCode(StatusCodes.Status201Created, new { url });
    }

    /// <summary>Fica fora do limite por IP: listas carregam dezenas de fotos e elas vão para o cache do navegador.</summary>
    [HttpGet("{nome}")]
    [AllowAnonymous]
    [DisableRateLimiting]
    public IActionResult Obter(string nome)
    {
        if (fotos.Localizar(nome) is not { } arquivo) return Erros.NaoEncontrado("Arquivo não encontrado.");

        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return PhysicalFile(arquivo.Caminho, arquivo.TipoConteudo);
    }
}
