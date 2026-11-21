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
public sealed class DispositivoController(DispositivoAppService dispositivo, ParadasDeAlarme paradas) : ControllerBase
{
    /// <summary>Enquanto toca, a placa pergunta se alguém parou o alarme no painel ou no app.</summary>
    [HttpGet("paradas")]
    public IActionResult Paradas()
    {
        dispositivo.Autorizar(ChaveEnviada);
        return Ok(paradas.Recentes());
    }

    /// <summary>Desligado no botão da placa: para também no painel e no app.</summary>
    [HttpPost("paradas")]
    public IActionResult Parar([FromBody] PararAlarmeDto dto)
    {
        dispositivo.Autorizar(ChaveEnviada);
        return Ok(new { marcados = paradas.Registrar(dto with { Origem = "caixinha" }, "caixinha") });
    }

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
