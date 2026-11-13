using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pawtinhas.API.Http;
using Pawtinhas.Application.Autenticacao;
using Pawtinhas.Application.Comum;
using Pawtinhas.Application.Services;

namespace Pawtinhas.API.Controllers;

/// <summary>Área do tutor logado: só os próprios animais e compromissos, somente leitura.</summary>
[ApiController]
[Route("api/meus")]
[Authorize(Policy = Politicas.Tutor)]
public sealed class TutorController(TutorAppService service) : ControllerBase
{
    /// <summary>O tutor vem do claim da sessão, nunca de parâmetro da requisição.</summary>
    private int ResponsavelId =>
        int.TryParse(User.FindFirst(Claims.Responsavel)?.Value, out var id) ? id : throw AutenticacaoException.SessaoExpirada();

    [HttpGet("animais")]
    public async Task<IActionResult> ListarAnimais() => Ok(await service.ListarAnimaisAsync(ResponsavelId));

    [HttpGet("animais/{id:int}")]
    public async Task<IActionResult> ObterAnimal(int id) =>
        await service.ObterAnimalAsync(ResponsavelId, id) is { } animal ? Ok(animal) : Erros.NaoEncontrado("Animal não encontrado.");

    [HttpGet("compromissos")]
    public async Task<IActionResult> ListarCompromissos([FromQuery] int? animalId) =>
        Ok(await service.ListarCompromissosAsync(ResponsavelId, animalId));
}