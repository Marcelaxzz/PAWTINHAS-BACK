using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pawtinhas.API.Http;
using Pawtinhas.Application.Autenticacao;

namespace Pawtinhas.API.Controllers;

/// <summary>Contas de acesso dos tutores. Só a veterinária cria, lista e exclui.</summary>
[ApiController]
[Route("api/contas-tutor")]
[Authorize(Policy = Politicas.Admin)]
public sealed class ContasTutorController(ServicoContas contas) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await contas.ListarAsync());

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarContaTutorRequisicao req) =>
        StatusCode(StatusCodes.Status201Created, await contas.CriarAsync(req));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id) =>
        await contas.ExcluirAsync(id) ? NoContent() : Erros.NaoEncontrado("Conta não encontrada.");
}