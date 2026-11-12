
using Microsoft.AspNetCore.Mvc;
using Pawtinhas.API.Http;
using Pawtinhas.Application.DTOs;
using Pawtinhas.Application.Services;

namespace Pawtinhas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResponsaveisController : ControllerBase
{
    private readonly ResponsavelAppService _service;

    public ResponsaveisController(ResponsavelAppService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Cadastrar(
        [FromBody] CadastrarResponsavelDto dto)
    {
        var responsavel = await _service.CadastrarAsync(dto);

        return CreatedAtAction(
            nameof(ObterPorId),
            new { id = responsavel.Id },
            responsavel);
    }

    [HttpGet]
    public async Task<IActionResult> ListarTodos()
    {
        var responsaveis = await _service.ListarAsync();

        return Ok(responsaveis);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObterPorId(int id)
    {
        var responsavel = await _service.ObterPorIdAsync(id);

        if (responsavel is null)
            return Erros.NaoEncontrado("Tutor não encontrado.");

        return Ok(responsavel);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(
        int id,
        [FromBody] CadastrarResponsavelDto dto)
    {
        var atualizado = await _service.AtualizarAsync(id, dto);

        if (atualizado is null)
            return Erros.NaoEncontrado("Tutor não encontrado.");

        return Ok(atualizado);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var removido = await _service.ExcluirAsync(id);

        if (!removido)
            return Erros.NaoEncontrado("Tutor não encontrado.");

        return NoContent();
    }
}
