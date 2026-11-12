using Microsoft.AspNetCore.Mvc;
using Pawtinhas.API.Http;
using Pawtinhas.Application.DTOs;
using Pawtinhas.Application.Relatorios;
using Pawtinhas.Application.Services;

namespace Pawtinhas.API.Controllers;

/// <summary>Estoque por lote dos medicamentos e das rações do catálogo.</summary>
[ApiController]
[Route("api/[controller]")]
public class EstoqueController(EstoqueAppService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await service.ListarAsync());

    [HttpGet("movimentacoes")]
    public async Task<IActionResult> Movimentacoes(
        [FromQuery] string? categoria, [FromQuery] int? itemId, [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate,
        [FromQuery] int limite = 200) =>
        Ok(await service.MovimentacoesAsync(categoria, itemId, de, ate, limite));

    [HttpPost("entradas")]
    public async Task<IActionResult> Entrada([FromBody] EntradaEstoqueDto dto) => Ok(await service.EntradaAsync(dto));

    [HttpPost("saidas")]
    public async Task<IActionResult> Saida([FromBody] SaidaEstoqueDto dto) => Ok(await service.SaidaAsync(dto));

    [HttpPost("descartes")]
    public async Task<IActionResult> Descarte([FromBody] DescarteEstoqueDto dto) => Ok(await service.DescarteAsync(dto));

    [HttpPut("minimo")]
    public async Task<IActionResult> Minimo([FromBody] MinimoEstoqueDto dto) => Ok(await service.DefinirMinimoAsync(dto));
}

/// <summary>Relatório que a veterinária preenche em cada atendimento da agenda.</summary>
[ApiController]
[Route("api/eventos/{eventoId:int}/relatorio")]
public class RelatorioServicoController(RelatorioServicoAppService service, RelatorioAppService pdf) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obter(int eventoId) =>
        await service.ObterAsync(eventoId) is { } r ? Ok(r) : Erros.NaoEncontrado("Este atendimento ainda não tem relatório.");

    [HttpPut]
    public async Task<IActionResult> Salvar(int eventoId, [FromBody] SalvarRelatorioServicoDto dto) =>
        await service.SalvarAsync(eventoId, dto) is { } r ? Ok(r) : Erros.NaoEncontrado("Atendimento não encontrado.");

    [HttpGet("pdf")]
    public async Task<IActionResult> Pdf(int eventoId) =>
        await pdf.AtendimentoAsync(eventoId) is { } arquivo ? Arquivo(arquivo) : Erros.NaoEncontrado("Atendimento não encontrado.");

    private FileContentResult Arquivo(ArquivoPdf a) => File(a.Conteudo, "application/pdf", a.Nome);
}

/// <summary>Relatórios automáticos em PDF.</summary>
[ApiController]
[Route("api/relatorios")]
public class RelatoriosController(RelatorioAppService service, RelatorioServicoAppService atendimentos, TimeProvider relogio)
    : ControllerBase
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().DateTime);

    /// <summary>Ids dos atendimentos que já têm relatório (a agenda marca quais faltam).</summary>
    [HttpGet("atendimentos-com-relatorio")]
    public async Task<IActionResult> ComRelatorio() => Ok(await atendimentos.EventosComRelatorioAsync());

    /// <summary>tipo: Consulta, Vacina, Cirurgia, BanhoTosa, PetSitter ou Emergencia (vazio: todos).</summary>
    [HttpGet("agenda")]
    public async Task<IActionResult> Agenda([FromQuery] DateOnly? de, [FromQuery] DateOnly? ate, [FromQuery] string? tipo) =>
        Arquivo(await service.AgendaAsync(de ?? Hoje.AddDays(-30), ate ?? Hoje, string.IsNullOrWhiteSpace(tipo) ? null : tipo));

    [HttpGet("estoque")]
    public async Task<IActionResult> Estoque([FromQuery] DateOnly? de, [FromQuery] DateOnly? ate) =>
        Arquivo(await service.EstoqueAsync(de ?? Hoje.AddDays(-30), ate ?? Hoje));

    [HttpGet("pacientes/{id:int}")]
    public async Task<IActionResult> Paciente(int id) =>
        await service.PacienteAsync(id) is { } a ? Arquivo(a) : Erros.NaoEncontrado("Paciente não encontrado.");

    [HttpGet("alarmes")]
    public async Task<IActionResult> Alarmes() => Arquivo(await service.AlarmesAsync());

    [HttpGet("adocao")]
    public async Task<IActionResult> Adocao() => Arquivo(await service.AdocaoAsync());

    private FileContentResult Arquivo(ArquivoPdf a) => File(a.Conteudo, "application/pdf", a.Nome);
}
