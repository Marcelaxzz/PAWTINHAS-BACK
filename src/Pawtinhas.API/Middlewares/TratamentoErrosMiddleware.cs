using Microsoft.EntityFrameworkCore;
using Pawtinhas.API.Http;
using Pawtinhas.Application.Comum;
using Pawtinhas.Domain.Comum;

namespace Pawtinhas.API.Middlewares;

/// <summary>
/// Converte as exceções dos serviços em respostas HTTP no formato { message, code, fields }.
/// Os controllers ficam enxutos (sem try/catch) e nenhum detalhe interno vaza nos erros 500.
/// </summary>
public sealed class TratamentoErrosMiddleware(RequestDelegate proximo, ILogger<TratamentoErrosMiddleware> log)
{
    public async Task InvokeAsync(HttpContext http)
    {
        try
        {
            await proximo(http);
        }
        catch (Exception ex) when (!http.Response.HasStarted && !http.RequestAborted.IsCancellationRequested)
        {
            await ResponderAsync(http, ex);
        }
    }

    private Task ResponderAsync(HttpContext http, Exception ex)
    {
        switch (ex)
        {
            case ValidacaoException v:
                return Erros.EscreverAsync(http, 400, "Confira os campos destacados.", "validacao", v.Erros);

            case DominioException d:
                return Erros.EscreverAsync(http, 400, d.Message, "validacao", new Dictionary<string, string> { [d.Campo] = d.Message });

            case AutenticacaoException a:
                if (a.RepetirApos is { } espera)
                    http.Response.Headers.RetryAfter = ((int)Math.Ceiling(espera.TotalSeconds)).ToString();
                return Erros.EscreverAsync(http, a.Status, a.Message, a.Codigo);

            case ConflitoException c:
                return Erros.EscreverAsync(http, 409, c.Message, c.Codigo);

            case KeyNotFoundException k:
                return Erros.EscreverAsync(http, 404, k.Message, "nao_encontrado");

            case ArgumentException arg:
                return Erros.EscreverAsync(http, 400, arg.Message, "invalido");

            case BadHttpRequestException bad:
                return Erros.EscreverAsync(http, bad.StatusCode, "Requisição inválida ou grande demais.", "requisicao_invalida");

            case DbUpdateException db:
                log.LogWarning(db, "Falha ao salvar em {Caminho}", http.Request.Path);
                return Erros.EscreverAsync(http, 409, "Não foi possível salvar: o registro está ligado a outros dados.", "conflito");

            default:
                log.LogError(ex, "Erro não tratado em {Caminho}", http.Request.Path);
                return Erros.EscreverAsync(http, 500, "Algo deu errado do nosso lado. Tente novamente em instantes.", "erro_interno");
        }
    }
}
