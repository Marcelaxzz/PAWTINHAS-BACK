using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Pawtinhas.API.Http;

/// <summary>Corpo padrão de erro, no formato que o frontend entende.</summary>
public sealed record RespostaErro(string Message, string Code, IReadOnlyDictionary<string, string>? Fields = null);

public static class Erros
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Para quem escreve a resposta direto (middlewares e eventos de autenticação).</summary>
    public static Task EscreverAsync(
        HttpContext http, int status, string mensagem, string codigo, IReadOnlyDictionary<string, string>? campos = null)
    {
        http.Response.StatusCode = status;
        http.Response.ContentType = "application/json; charset=utf-8";
        return http.Response.WriteAsync(JsonSerializer.Serialize(new RespostaErro(mensagem, codigo, campos), Json));
    }

    public static ObjectResult NaoEncontrado(string mensagem = "Registro não encontrado.") =>
        new(new RespostaErro(mensagem, "nao_encontrado")) { StatusCode = StatusCodes.Status404NotFound };

    /// <summary>"Prescricoes.0.DoseMgKg" → "doseMgKg": o mesmo nome do campo no JSON.</summary>
    public static string Campo(string caminho)
    {
        var ultimo = caminho.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "geral";
        ultimo = ultimo.TrimStart('$').Split('[')[0];
        return ultimo.Length == 0 ? "geral" : JsonNamingPolicy.CamelCase.ConvertName(ultimo);
    }
}
