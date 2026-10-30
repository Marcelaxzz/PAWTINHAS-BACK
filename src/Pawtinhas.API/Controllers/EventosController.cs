using Microsoft.AspNetCore.Mvc;
using Pawtinhas.Application.DTOs.Eventos;
using Pawtinhas.Application.Services;

namespace Pawtinhas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventosController : ControllerBase
{
    private readonly EventoAppService _service;

    public EventosController(EventoAppService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Cadastrar(
        [FromBody] CadastrarEventoDto dto)
    {
        try
        {
            var evento = await _service.CadastrarAsync(dto);

            return CreatedAtAction(
                nameof(ObterPorId),
                new { id = evento.Id },
                evento);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var eventos = await _service.ListarAsync();

        return Ok(eventos);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObterPorId(int id)
    {
        try
        {
            var evento = await _service.ObterPorIdAsync(id);

            return Ok(evento);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }
}