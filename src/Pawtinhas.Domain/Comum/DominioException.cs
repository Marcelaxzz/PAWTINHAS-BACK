namespace Pawtinhas.Domain.Comum;

/// <summary>
/// Regra de negócio violada. <see cref="Campo"/> é o nome do campo no JSON (camelCase),
/// para a tela destacar o campo certo. Herda de ArgumentException, então os tratamentos
/// que já capturam ArgumentException continuam funcionando.
/// </summary>
public class DominioException(string campo, string mensagem) : ArgumentException(mensagem)
{
    public string Campo { get; } = campo;
}

/// <summary>Vários campos inválidos de uma vez (formulários de cálculo, por exemplo).</summary>
public class ValidacaoException(IReadOnlyDictionary<string, string> erros)
    : ArgumentException(erros.Values.FirstOrDefault() ?? "Confira os dados informados.")
{
    public IReadOnlyDictionary<string, string> Erros { get; } = erros;

    /// <summary>Lança se houver algum erro acumulado.</summary>
    public static void SeHouver(IReadOnlyDictionary<string, string> erros)
    {
        if (erros.Count > 0) throw new ValidacaoException(erros);
    }
}

/// <summary>Operação válida, mas que precisa de confirmação ou conflita com o estado atual (HTTP 409).</summary>
public class ConflitoException(string codigo, string mensagem) : InvalidOperationException(mensagem)
{
    public string Codigo { get; } = codigo;
}
