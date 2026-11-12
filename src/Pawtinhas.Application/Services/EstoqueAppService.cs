using Pawtinhas.Application.DTOs;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Comum;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Services;

/// <summary>
/// Estoque dos medicamentos, das rações e dos materiais do catálogo, por lote. Medicamento conta na
/// unidade de administração (comprimidos, mL...); ração conta em kg; material na unidade cadastrada.
/// </summary>
public class EstoqueAppService(
    IEstoqueRepository estoque,
    IRepository<Medicamento> medicamentos,
    IRepository<Racao> racoes,
    IRepository<Material> materiais,
    IUnitOfWork unitOfWork,
    TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().DateTime);

    public async Task<IReadOnlyList<ItemEstoqueDto>> ListarAsync()
    {
        var itens = (await estoque.ListarAsync()).ToDictionary(i => (i.Categoria, i.ItemId));
        var hoje = Hoje;
        var lista = new List<ItemEstoqueDto>();

        foreach (var m in await medicamentos.ObterTodosAsync())
            lista.Add(Montar(CategoriaEstoque.Medicamento, m.Id, m.Nome, $"{m.PrincipioAtivo} · {m.Forma}",
                UnidadePlural(m.Unidade), itens.GetValueOrDefault((CategoriaEstoque.Medicamento, m.Id)), hoje));
        foreach (var r in await racoes.ObterTodosAsync())
            lista.Add(Montar(CategoriaEstoque.Racao, r.Id, r.Nome, $"{r.Tipo} · {r.Especie} · {r.Fase}", "kg",
                itens.GetValueOrDefault((CategoriaEstoque.Racao, r.Id)), hoje));
        foreach (var m in await materiais.ObterTodosAsync())
            lista.Add(Montar(CategoriaEstoque.Material, m.Id, m.Nome,
                string.IsNullOrEmpty(m.Especificacao) ? m.Tipo : $"{m.Tipo} · {m.Especificacao}",
                UnidadePlural(m.Unidade), itens.GetValueOrDefault((CategoriaEstoque.Material, m.Id)), hoje));

        return lista.OrderBy(i => i.Categoria).ThenBy(i => i.Nome).ToList();
    }

    public async Task<IReadOnlyList<MovimentacaoEstoqueDto>> MovimentacoesAsync(
        string? categoria, int? itemId, DateOnly? de, DateOnly? ate, int limite = 200) =>
        (await estoque.MovimentacoesAsync(
            categoria, itemId, de?.ToDateTime(TimeOnly.MinValue).ToUniversalTime(),
            ate?.AddDays(1).ToDateTime(TimeOnly.MinValue).ToUniversalTime(), Math.Clamp(limite, 1, 1000)))
        .Select(m => new MovimentacaoEstoqueDto(m.Id, m.Item!.Categoria, m.Item.ItemId, m.Tipo, m.Quantidade, m.Motivo,
            m.Lotes, m.SaldoApos, DateTime.SpecifyKind(m.CriadoEm, DateTimeKind.Utc)))
        .ToList();

    public async Task<ItemEstoqueDto> EntradaAsync(EntradaEstoqueDto dto)
    {
        var item = await ObterOuCriarAsync(dto.Categoria, dto.ItemId!.Value);
        var mov = item.Entrada(dto.Quantidade ?? 0, dto.Lote, dto.Validade, dto.Motivo, Hoje);
        await estoque.AdicionarMovimentacaoAsync(mov);
        await unitOfWork.CommitAsync();
        return await ItemAsync(dto.Categoria, dto.ItemId.Value);
    }

    public async Task<ItemEstoqueDto> SaidaAsync(SaidaEstoqueDto dto)
    {
        var item = await ObterOuCriarAsync(dto.Categoria, dto.ItemId!.Value);
        var mov = item.Saida(dto.Quantidade ?? 0, dto.Motivo);
        await estoque.AdicionarMovimentacaoAsync(mov);
        await unitOfWork.CommitAsync();
        return await ItemAsync(dto.Categoria, dto.ItemId.Value);
    }

    public async Task<ItemEstoqueDto> DescarteAsync(DescarteEstoqueDto dto)
    {
        var item = await ObterOuCriarAsync(dto.Categoria, dto.ItemId!.Value);
        var mov = item.Descartar(dto.LoteId!.Value, dto.Motivo);
        await estoque.AdicionarMovimentacaoAsync(mov);
        await unitOfWork.CommitAsync();
        return await ItemAsync(dto.Categoria, dto.ItemId.Value);
    }

    public async Task<ItemEstoqueDto> DefinirMinimoAsync(MinimoEstoqueDto dto)
    {
        var item = await ObterOuCriarAsync(dto.Categoria, dto.ItemId!.Value);
        item.DefinirMinimo(dto.Minimo);
        await unitOfWork.CommitAsync();
        return await ItemAsync(dto.Categoria, dto.ItemId.Value);
    }

    private async Task<ItemEstoqueDto> ItemAsync(string categoria, int itemId) =>
        (await ListarAsync()).First(i => i.Categoria == categoria && i.ItemId == itemId);

    private async Task<ItemEstoque> ObterOuCriarAsync(string? categoria, int itemId)
    {
        var cat = CategoriaEstoque.Validar(categoria);
        var existe = cat switch
        {
            CategoriaEstoque.Medicamento => await medicamentos.ObterPorIdAsync(itemId) is not null,
            CategoriaEstoque.Racao => await racoes.ObterPorIdAsync(itemId) is not null,
            _ => await materiais.ObterPorIdAsync(itemId) is not null,
        };
        if (!existe) throw new DominioException("itemId", "Item não encontrado no catálogo.");

        if (await estoque.ObterAsync(cat, itemId) is { } item) return item;
        item = new ItemEstoque(cat, itemId);
        await estoque.AdicionarAsync(item);
        return item;
    }

    private static ItemEstoqueDto Montar(
        string categoria, int itemId, string nome, string detalhe, string unidade, ItemEstoque? item, DateOnly hoje)
    {
        var lotes = (item?.Lotes ?? [])
            .Where(l => l.Quantidade > 0)
            .OrderBy(l => l.Validade ?? DateOnly.MaxValue)
            .Select(l => LoteEstoqueDto.FromEntity(l, hoje))
            .ToList();
        var saldo = lotes.Sum(l => l.Quantidade);
        return new ItemEstoqueDto(categoria, itemId, nome, detalhe, unidade, saldo, item?.Minimo,
            lotes.Where(l => l.Validade is not null).Select(l => l.Validade).FirstOrDefault(),
            SituacaoEstoque.Calcular(saldo, item?.Minimo, lotes), lotes);
    }

    /// <summary>"comprimido" → "comprimidos", para a tela e o relatório.</summary>
    public static string UnidadePlural(string unidade) => unidade switch
    {
        "comprimido" => "comprimidos",
        "cápsula" => "cápsulas",
        "gota" => "gotas",
        "unidade" => "unidades",
        "pacote" => "pacotes",
        "caixa" => "caixas",
        "frasco" => "frascos",
        "bolsa" => "bolsas",
        "rolo" => "rolos",
        "par" => "pares",
        "kit" => "kits",
        _ => unidade,
    };
}
