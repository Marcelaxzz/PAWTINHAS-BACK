namespace Pawtinhas.Domain.Seguranca;

/// <summary>Regras de força da senha (as mesmas exibidas no frontend).</summary>
public static class PoliticaSenha
{
    public const int Minimo = 8;
    public const int Maximo = 128;

    /// <summary>Retorna o que falta na senha, ou nulo quando ela atende a todas as regras.</summary>
    public static string? ProblemaDeForca(string? senha)
    {
        if (string.IsNullOrEmpty(senha)) return "Crie uma senha.";
        if (senha.Length > Maximo) return $"Use no máximo {Maximo} caracteres.";

        var faltando = new List<string>();
        if (senha.Length < Minimo) faltando.Add($"{Minimo}+ caracteres");
        if (!senha.Any(char.IsUpper)) faltando.Add("letra maiúscula");
        if (!senha.Any(char.IsLower)) faltando.Add("letra minúscula");
        if (!senha.Any(char.IsDigit)) faltando.Add("número");
        if (senha.All(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))) faltando.Add("símbolo");

        return faltando.Count == 0 ? null : $"A senha precisa de: {string.Join(", ", faltando)}.";
    }
}
