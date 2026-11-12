namespace Pawtinhas.Domain.Comum;

/// <summary>Validações usadas pelas entidades. Toda falha vira uma <see cref="DominioException"/>.</summary>
public static class Validar
{
    public static string Texto(string? valor, string campo, int maximo, string mensagemObrigatorio)
    {
        var texto = valor?.Trim() ?? "";
        if (texto.Length == 0) throw new DominioException(campo, mensagemObrigatorio);
        if (texto.Length > maximo) throw new DominioException(campo, $"Use no máximo {maximo} caracteres.");
        return texto;
    }

    public static string? TextoOpcional(string? valor, string campo, int maximo)
    {
        var texto = valor?.Trim();
        if (string.IsNullOrEmpty(texto)) return null;
        if (texto.Length > maximo) throw new DominioException(campo, $"Use no máximo {maximo} caracteres.");
        return texto;
    }

    /// <summary>Como <see cref="TextoOpcional"/>, mas devolve "" em vez de nulo (colunas não nulas).</summary>
    public static string TextoLivre(string? valor, string campo, int maximo) => TextoOpcional(valor, campo, maximo) ?? "";

    public static string Opcao(string? valor, IReadOnlyCollection<string> opcoes, string campo, string mensagem)
    {
        var texto = valor?.Trim() ?? "";
        return opcoes.Contains(texto) ? texto : throw new DominioException(campo, mensagem);
    }

    public static decimal Faixa(decimal valor, decimal minimo, decimal maximo, string campo, string mensagem) =>
        valor >= minimo && valor <= maximo ? valor : throw new DominioException(campo, mensagem);

    public static int Faixa(int valor, int minimo, int maximo, string campo, string mensagem) =>
        valor >= minimo && valor <= maximo ? valor : throw new DominioException(campo, mensagem);
}
