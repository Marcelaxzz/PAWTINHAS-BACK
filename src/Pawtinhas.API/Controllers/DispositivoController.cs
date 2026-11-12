using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pawtinhas.Application.DTOs;
using Pawtinhas.Application.Services;

namespace Pawtinhas.API.Controllers;

/// <summary>
/// Integração com o alarme ESP32. O dispositivo envia a chave no cabeçalho X-Device-Key
/// (sem cookies, por isso fica fora do CSRF). Sem Dispositivo:Chave configurada, responde 404.
/// </summary>
[ApiController]
[Route("api/dispositivo")]
[AllowAnonymous]
public sealed class DispositivoController(DispositivoAppService dispositivo) : ControllerBase
{
    private string ChaveEnviada => Request.Headers["X-Device-Key"].ToString();

    [HttpGet("alarmes")]
    public async Task<IActionResult> Alarmes()
    {
        dispositivo.Autorizar(ChaveEnviada);
        return Ok(await dispositivo.AlarmesAsync());
    }

    [HttpPost("disparos")]
    public async Task<IActionResult> RegistrarDisparo([FromBody] DisparoDispositivoDto dto)
    {
        dispositivo.Autorizar(ChaveEnviada);
        await dispositivo.RegistrarDisparoAsync(dto);
        return Ok(new { ok = true });
    }
}
