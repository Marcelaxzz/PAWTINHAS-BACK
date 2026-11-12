using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pawtinhas.API.Http;
using Pawtinhas.Application.DTOs;
using Pawtinhas.Application.Services;

namespace Pawtinhas.API.Controllers;

/// <summary>
/// Vitrine de adoção. Listagem e detalhe são públicos (sem os já adotados e sem o adotante);
/// cadastro, edição e exclusão exigem a sessão da veterinária.
/// </summary>
[ApiController]
[Route("api/adocao")]
public class AdocaoController(AdocaoAppService service) : ControllerBase
{
    private bool Autenticado => User.Identity?.IsAuthenticated == true;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Listar() => Ok(await service.ListarAsync(Autenticado));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Obter(int id) =>
        await service.ObterAsync(id, Autenticado) is { } animal ? Ok(animal) : Erros.NaoEncontrado("Animal não encontrado.");

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] SalvarAnimalAdocaoDto dto)
    {
        var animal = await service.CadastrarAsync(dto);
        return CreatedAtAction(nameof(Obter), new { id = animal.Id }, animal);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] SalvarAnimalAdocaoDto dto) =>
        await service.AtualizarAsync(id, dto) is { } animal ? Ok(animal) : Erros.NaoEncontrado("Animal não encontrado.");

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id) =>
        await service.ExcluirAsync(id) ? NoContent() : Erros.NaoEncontrado("Animal não encontrado.");
}
