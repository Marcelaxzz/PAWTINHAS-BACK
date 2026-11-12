using Microsoft.AspNetCore.Mvc;
using Pawtinhas.Application.DTOs.Eventos;
using Pawtinhas.Application.Services;

namespace Pawtinhas.API.Controllers;

/// <summary>Agenda: consultas, vacinas, cirurgias, banho e tosa, pet sitter e emergências.</summary>
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
        var evento = await _service.CadastrarAsync(dto);

        return CreatedAtAction(
            nameof(ObterPorId),
            new { id = evento.Id },
            evento);
    }

    /// <param name="tipo">consulta, vacina, cirurgia, banhotosa, petsitter ou emergencia.</param>
    /// <param name="animalId">Só os compromissos de um paciente (ficha).</param>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? tipo, [FromQuery] int? animalId)
    {
        var eventos = await _service.ListarAsync(tipo, animalId);

        return Ok(eventos);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObterPorId(int id)
    {
        var evento = await _service.ObterPorIdAsync(id);

        return Ok(evento);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] CadastrarEventoDto dto) =>
        Ok(await _service.AtualizarAsync(id, dto));

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> AlterarStatus(int id, [FromBody] AlterarStatusEventoDto dto) =>
        Ok(await _service.AlterarStatusAsync(id, dto.Status));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        await _service.ExcluirAsync(id);
        return NoContent();
    }
}
