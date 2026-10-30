
using Microsoft.AspNetCore.Mvc;
using Pawtinhas.Application.DTOs;
using Pawtinhas.Application.Services;

namespace Pawtinhas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnimalsController : ControllerBase
{
    private readonly AnimalAppService _service;

    public AnimalsController(AnimalAppService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Cadastrar(
        [FromBody] CadastrarAnimalDto dto)
    {
        var animal = await _service.CadastrarAnimalAsync(dto);

        return CreatedAtAction(
            nameof(ObterPorId),
            new { id = animal.Id },
            animal);
    }

    [HttpGet]
    public async Task<IActionResult> ListarTodos()
    {
        var animais = await _service.ListarAnimaisAsync();

        return Ok(animais);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObterPorId(int id)
    {
        var animal = await _service.ObterAnimalPorIdAsync(id);

        if (animal is null)
            return NotFound();

        return Ok(animal);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(
        int id,
        [FromBody] CadastrarAnimalDto dto)
    {
        var atualizado = await _service.AtualizarAnimalAsync(id, dto);

        if (!atualizado)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var removido = await _service.ExcluirAnimalAsync(id);

        if (!removido)
            return NotFound();

        return NoContent();
    }
}