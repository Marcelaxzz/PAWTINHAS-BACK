using System.ComponentModel.DataAnnotations;
using Pawtinhas.Domain;

namespace Pawtinhas.Application.DTOs;

// ---------- Estoque ----------

public sealed record LoteEstoqueDto(int Id, string Codigo, DateOnly? Validade, decimal Quantidade, bool Vencido, bool VenceEmBreve)
{
    public static LoteEstoqueDto FromEntity(LoteEstoque l, DateOnly hoje) => new(
        l.Id, l.Codigo, l.Validade, l.Quantidade,
        l.Validade is { } v && v < hoje,
        l.Validade is { } w && w >= hoje && w <= hoje.AddDays(SituacaoEstoque.DiasAvisoValidade));
}

/// <summary>Posição de um item do catálogo no estoque (itens sem entrada aparecem com saldo zero).</summary>
public sealed record ItemEstoqueDto(
    string Categoria, int ItemId, string Nome, string Detalhe, string Unidade, decimal Saldo, decimal? Minimo,
    DateOnly? ProximaValidade, string Situacao, IReadOnlyList<LoteEstoqueDto> Lotes);

public sealed record MovimentacaoEstoqueDto(
    int Id, string Categoria, int ItemId, string Tipo, decimal Quantidade, string Motivo, string Lotes, decimal SaldoApos,
    DateTime CriadoEm);

public sealed record EntradaEstoqueDto(
    [Required(ErrorMessage = "Escolha medicamento ou ração.")] string Categoria,
    [Required(ErrorMessage = "Escolha o item.")] int? ItemId,
    [Required(ErrorMessage = "Informe a quantidade.")] decimal? Quantidade,
    [StringLength(40)] string? Lote,
    DateOnly? Validade,
    [StringLength(200)] string? Motivo);

public sealed record SaidaEstoqueDto(
    [Required(ErrorMessage = "Escolha medicamento ou ração.")] string Categoria,
    [Required(ErrorMessage = "Escolha o item.")] int? ItemId,
    [Required(ErrorMessage = "Informe a quantidade.")] decimal? Quantidade,
    [StringLength(200)] string? Motivo);

public sealed record DescarteEstoqueDto(
    [Required(ErrorMessage = "Escolha medicamento ou ração.")] string Categoria,
    [Required(ErrorMessage = "Escolha o item.")] int? ItemId,
    [Required(ErrorMessage = "Escolha o lote.")] int? LoteId,
    [StringLength(200)] string? Motivo);

public sealed record MinimoEstoqueDto(
    [Required(ErrorMessage = "Escolha medicamento ou ração.")] string Categoria,
    [Required(ErrorMessage = "Escolha o item.")] int? ItemId,
    decimal? Minimo);

/// <summary>Regras de situação mostradas na tela e nos relatórios.</summary>
public static class SituacaoEstoque
{
    public const int DiasAvisoValidade = 30;

    public const string Zerado = "Zerado";
    public const string Baixo = "Baixo";
    public const string Vencido = "Vencido";
    public const string Vencendo = "Vencendo";
    public const string Normal = "Normal";

    public static string Calcular(decimal saldo, decimal? minimo, IEnumerable<LoteEstoqueDto> lotes)
    {
        if (saldo <= 0) return Zerado;
        if (lotes.Any(l => l.Vencido)) return Vencido;
        if (minimo is { } m && saldo <= m) return Baixo;
        if (lotes.Any(l => l.VenceEmBreve)) return Vencendo;
        return Normal;
    }
}

// ---------- Relatório de serviço ----------

public sealed record SalvarRelatorioServicoDto(
    [StringLength(1000)] string? Queixa,
    [StringLength(2000)] string? Avaliacao,
    [StringLength(2000)] string? Procedimentos,
    [StringLength(1000)] string? Diagnostico,
    [StringLength(2000)] string? Orientacoes,
    decimal? PesoKg,
    decimal? TemperaturaC,
    DateOnly? Retorno,
    [StringLength(120)] string? Responsavel);

public sealed record RelatorioServicoDto(
    int Id, int EventoId, string Queixa, string Avaliacao, string Procedimentos, string Diagnostico, string Orientacoes,
    decimal? PesoKg, decimal? TemperaturaC, DateOnly? Retorno, string Responsavel, DateTime CriadoEm, DateTime AtualizadoEm)
{
    public static RelatorioServicoDto FromEntity(RelatorioServico r) => new(
        r.Id, r.EventoId, r.Queixa, r.Avaliacao, r.Procedimentos, r.Diagnostico, r.Orientacoes, r.PesoKg, r.TemperaturaC,
        r.Retorno, r.Responsavel, r.CriadoEm, r.AtualizadoEm);
}
