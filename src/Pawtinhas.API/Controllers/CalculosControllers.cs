using Microsoft.AspNetCore.Mvc;
using Pawtinhas.API.Http;
using Pawtinhas.Application.DTOs;
using Pawtinhas.Application.Services;

namespace Pawtinhas.API.Controllers;

/// <summary>Simulações: dose de medicamento e alimentação (nada é salvo). Fatores e fontes da tabela.</summary>
[ApiController]
[Route("api/[controller]")]
public class CalculosController(CalculoAppService service) : ControllerBase
{
    [HttpGet("fatores")]
    public IActionResult Fatores() => Ok(CalculoAppService.Fatores());

    [HttpPost("dose")]
    public async Task<IActionResult> Dose([FromBody] DoseRequisicao req) => Ok(await service.SimularDoseAsync(req));

    [HttpPost("alimentacao")]
    public async Task<IActionResult> Alimentacao([FromBody] AlimentacaoRequisicao req) =>
        Ok(await service.SimularAlimentacaoAsync(req));
}

/// <summary>Prescrições salvas na ficha (a dose é recalculada pela API a partir do catálogo).</summary>
[ApiController]
[Route("api/[controller]")]
public class PrescricoesController(PrescricaoAppService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? animalId) => Ok(await service.ListarAsync(animalId));

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] DoseRequisicao req) =>
        StatusCode(StatusCodes.Status201Created, await service.CriarAsync(req));

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] PrescricaoAlteracao req) =>
        await service.AtualizarAsync(id, req) is { } p ? Ok(p) : Erros.NaoEncontrado("Prescrição não encontrada.");

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id) =>
        await service.ExcluirAsync(id) ? NoContent() : Erros.NaoEncontrado("Prescrição não encontrada.");
}

[ApiController]
[Route("api/planos-alimentares")]
public class PlanosAlimentaresController(PlanoAlimentarAppService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? animalId) => Ok(await service.ListarAsync(animalId));

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] AlimentacaoRequisicao req) =>
        StatusCode(StatusCodes.Status201Created, await service.CriarAsync(req));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id) =>
        await service.ExcluirAsync(id) ? NoContent() : Erros.NaoEncontrado("Plano não encontrado.");
}

/// <summary>Configuração única da clínica, status das integrações e backup.</summary>
[ApiController]
[Route("api/[controller]")]
public class ConfiguracoesController(ConfiguracaoAppService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obter() => Ok(await service.ObterAsync());

    [HttpPut]
    public async Task<IActionResult> Atualizar([FromBody] ConfiguracaoDto dto) => Ok(await service.AtualizarAsync(dto));

    [HttpGet("status")]
    public async Task<IActionResult> Status() => Ok(await service.StatusAsync());

    /// <summary>Cópia completa dos dados em JSON, para guardar em local seguro.</summary>
    [HttpGet("backup")]
    public async Task<IActionResult> Backup(CancellationToken ct) =>
        File(await service.GerarBackupAsync(ct), "application/json", $"pawtinhas-backup-{DateTime.Now:yyyyMMdd-HHmm}.json");
}
