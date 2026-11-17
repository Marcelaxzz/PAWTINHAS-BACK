using Microsoft.AspNetCore.Mvc;
using Pawtinhas.API.Http;
using Pawtinhas.Application.DTOs;
using Pawtinhas.Application.Services;

namespace Pawtinhas.API.Controllers;

/// <summary>Equipe da clínica (não são logins: aparecem nos agendamentos conforme a função).</summary>
[ApiController]
[Route("api/[controller]")]
public class ProfissionaisController(ProfissionalAppService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await service.ListarAsync());

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] SalvarProfissionalDto dto) =>
        StatusCode(StatusCodes.Status201Created, await service.CadastrarAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] SalvarProfissionalDto dto) =>
        await service.AtualizarAsync(id, dto) is { } p ? Ok(p) : Erros.NaoEncontrado("Profissional não encontrado.");

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id) =>
        await service.ExcluirAsync(id) ? NoContent() : Erros.NaoEncontrado("Profissional não encontrado.");
}

/// <summary>Catálogo de medicamentos usado na calculadora de dose.</summary>
[ApiController]
[Route("api/[controller]")]
public class MedicamentosController(MedicamentoAppService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await service.ListarAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int id) =>
        await service.ObterAsync(id) is { } m ? Ok(m) : Erros.NaoEncontrado("Medicamento não encontrado.");

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] SalvarMedicamentoDto dto) =>
        StatusCode(StatusCodes.Status201Created, await service.CadastrarAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] SalvarMedicamentoDto dto) =>
        await service.AtualizarAsync(id, dto) is { } m ? Ok(m) : Erros.NaoEncontrado("Medicamento não encontrado.");

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id) =>
        await service.ExcluirAsync(id) ? NoContent() : Erros.NaoEncontrado("Medicamento não encontrado.");
}

/// <summary>Catálogo de rações usado na calculadora de alimentação.</summary>
[ApiController]
[Route("api/[controller]")]
public class RacoesController(RacaoAppService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await service.ListarAsync());

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] SalvarRacaoDto dto) =>
        StatusCode(StatusCodes.Status201Created, await service.CadastrarAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] SalvarRacaoDto dto) =>
        await service.AtualizarAsync(id, dto) is { } r ? Ok(r) : Erros.NaoEncontrado("Ração não encontrada.");

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id) =>
        await service.ExcluirAsync(id) ? NoContent() : Erros.NaoEncontrado("Ração não encontrada.");
}

/// <summary>Materiais e insumos (curativos, soro, instrumental) com controle de estoque.</summary>
[ApiController]
[Route("api/[controller]")]
public class MateriaisController(MaterialAppService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await service.ListarAsync());

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] SalvarMaterialDto dto) =>
        StatusCode(StatusCodes.Status201Created, await service.CadastrarAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] SalvarMaterialDto dto) =>
        await service.AtualizarAsync(id, dto) is { } m ? Ok(m) : Erros.NaoEncontrado("Material não encontrado.");

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id) =>
        await service.ExcluirAsync(id) ? NoContent() : Erros.NaoEncontrado("Material não encontrado.");
}

/// <summary>Alarmes tocados pelo ESP32 e o histórico de disparos.</summary>
[ApiController]
[Route("api/[controller]")]
public class AlarmesController(AlarmeAppService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await service.ListarAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int id) =>
        await service.ObterAsync(id) is { } a ? Ok(a) : Erros.NaoEncontrado("Alarme não encontrado.");

    [HttpGet("historico")]
    public async Task<IActionResult> Historico() => Ok(await service.HistoricoAsync());

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] SalvarAlarmeDto dto) =>
        StatusCode(StatusCodes.Status201Created, await service.CadastrarAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] SalvarAlarmeDto dto) =>
        await service.AtualizarAsync(id, dto) is { } a ? Ok(a) : Erros.NaoEncontrado("Alarme não encontrado.");

    [HttpPatch("{id:int}/ativo")]
    public async Task<IActionResult> AlterarAtivo(int id, [FromBody] AlterarAtivoAlarmeDto dto) =>
        await service.AlterarAtivoAsync(id, dto.Ativo) is { } a ? Ok(a) : Erros.NaoEncontrado("Alarme não encontrado.");

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id) =>
        await service.ExcluirAsync(id) ? NoContent() : Erros.NaoEncontrado("Alarme não encontrado.");
}

