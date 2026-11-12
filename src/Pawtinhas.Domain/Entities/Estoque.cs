using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>Categorias de item do estoque (cada uma aponta para o catálogo correspondente).</summary>
public static class CategoriaEstoque
{
    public const string Medicamento = "Medicamento";
    public const string Racao = "Racao";
    public const string Material = "Material";

    public static readonly string[] Todas = [Medicamento, Racao, Material];

    public static string Validar(string? categoria) =>
        Comum.Validar.Opcao(categoria, Todas, "categoria", "Escolha medicamento, ração ou material.");
}

/// <summary>
/// Configuração de estoque de um item do catálogo: o mínimo que dispara o aviso de reposição.
/// Fica separada do medicamento e da ração para o catálogo continuar igual para a calculadora.
/// </summary>
public class ItemEstoque
{
    public int Id { get; private set; }
    public string Categoria { get; private set; } = string.Empty;
    public int ItemId { get; private set; }

    /// <summary>Abaixo deste saldo o item aparece como "estoque baixo". Nulo: sem aviso.</summary>
    public decimal? Minimo { get; private set; }

    public List<LoteEstoque> Lotes { get; private set; } = [];

    private ItemEstoque() { }

    public ItemEstoque(string categoria, int itemId)
    {
        Categoria = CategoriaEstoque.Validar(categoria);
        ItemId = itemId;
    }

    public decimal Saldo => Lotes.Sum(l => l.Quantidade);

    public void DefinirMinimo(decimal? minimo) =>
        Minimo = minimo is { } m ? Validar.Faixa(m, 0m, 1_000_000m, "minimo", "Informe um mínimo entre 0 e 1.000.000.") : null;

    /// <summary>Entrada de um lote novo (compra, doação). Lote e validade são opcionais.</summary>
    public MovimentacaoEstoque Entrada(decimal quantidade, string? codigoLote, DateOnly? validade, string? motivo, DateOnly hoje)
    {
        Validar.Faixa(quantidade, 0.001m, 1_000_000m, "quantidade", "Informe uma quantidade maior que zero.");
        if (validade is { } v && v < hoje.AddYears(-1))
            throw new DominioException("validade", "Confira a validade: ela venceu há mais de um ano.");

        var lote = new LoteEstoque(codigoLote, validade, quantidade);
        Lotes.Add(lote);
        return new MovimentacaoEstoque(this, MovimentacaoEstoque.Entrada, quantidade,
            Validar.TextoOpcional(motivo, "motivo", 200) ?? "Entrada", lote.Codigo, Saldo);
    }

    /// <summary>
    /// Saída (uso em atendimento, venda, perda). Consome primeiro o lote que vence antes (FEFO);
    /// lotes sem validade ficam por último.
    /// </summary>
    public MovimentacaoEstoque Saida(decimal quantidade, string? motivo)
    {
        Validar.Faixa(quantidade, 0.001m, 1_000_000m, "quantidade", "Informe uma quantidade maior que zero.");
        if (quantidade > Saldo)
            throw new DominioException("quantidade", $"Só há {Saldo:0.##} em estoque.");

        var restante = quantidade;
        var usados = new List<string>();
        foreach (var lote in Lotes.Where(l => l.Quantidade > 0).OrderBy(l => l.Validade ?? DateOnly.MaxValue).ThenBy(l => l.Id))
        {
            if (restante <= 0) break;
            var tirar = Math.Min(restante, lote.Quantidade);
            lote.Retirar(tirar);
            restante -= tirar;
            if (!string.IsNullOrEmpty(lote.Codigo)) usados.Add(lote.Codigo);
        }
        Lotes.RemoveAll(l => l.Quantidade <= 0);

        return new MovimentacaoEstoque(this, MovimentacaoEstoque.Saida, quantidade,
            Validar.TextoOpcional(motivo, "motivo", 200) ?? "Saída", string.Join(", ", usados), Saldo);
    }

    /// <summary>Descarte de um lote inteiro (vencido, avariado).</summary>
    public MovimentacaoEstoque Descartar(int loteId, string? motivo)
    {
        var lote = Lotes.FirstOrDefault(l => l.Id == loteId)
            ?? throw new DominioException("loteId", "Lote não encontrado neste item.");
        var quantidade = lote.Quantidade;
        Lotes.Remove(lote);
        return new MovimentacaoEstoque(this, MovimentacaoEstoque.Descarte, quantidade,
            Validar.TextoOpcional(motivo, "motivo", 200) ?? "Descarte", lote.Codigo, Saldo);
    }
}

/// <summary>Lote em estoque; <see cref="Quantidade"/> é o que ainda resta dele.</summary>
public class LoteEstoque
{
    public int Id { get; private set; }
    public int ItemEstoqueId { get; private set; }
    public string Codigo { get; private set; } = string.Empty;
    public DateOnly? Validade { get; private set; }
    public decimal Quantidade { get; private set; }
    public DateTime CriadoEm { get; private set; } = DateTime.UtcNow;

    private LoteEstoque() { }

    internal LoteEstoque(string? codigo, DateOnly? validade, decimal quantidade)
    {
        Codigo = Validar.TextoLivre(codigo, "lote", 40);
        Validade = validade;
        Quantidade = quantidade;
    }

    internal void Retirar(decimal quantidade) => Quantidade -= quantidade;
}

/// <summary>Histórico do estoque: cada entrada, saída ou descarte com o saldo que ficou.</summary>
public class MovimentacaoEstoque
{
    public const string Entrada = "Entrada";
    public const string Saida = "Saída";
    public const string Descarte = "Descarte";

    public int Id { get; private set; }
    public int ItemEstoqueId { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public decimal Quantidade { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public string Lotes { get; private set; } = string.Empty;
    public decimal SaldoApos { get; private set; }
    public DateTime CriadoEm { get; private set; } = DateTime.UtcNow;

    public ItemEstoque? Item { get; private set; }

    private MovimentacaoEstoque() { }

    internal MovimentacaoEstoque(ItemEstoque item, string tipo, decimal quantidade, string motivo, string lotes, decimal saldoApos)
    {
        Item = item;
        Tipo = tipo;
        Quantidade = quantidade;
        Motivo = motivo;
        Lotes = lotes.Length > 200 ? lotes[..200] : lotes;
        SaldoApos = saldoApos;
    }
}
