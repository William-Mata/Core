using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

[ApiController]
[Route("api/financeiro/faturas-cartao")]
[Authorize]
public sealed class FaturaCartaoController(FaturaCartaoService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] long? cartaoId,
        [FromQuery] string competencia,
        CancellationToken cancellationToken)
    {
        return Ok(await service.ListarAsync(new ListarFaturasCartaoRequest(cartaoId, competencia), cancellationToken));
    }

    [HttpGet("detalhes")]
    public async Task<IActionResult> ListarDetalhes(
        [FromQuery] string competencia,
        [FromQuery] string? tipoTransacao,
        CancellationToken cancellationToken)
    {
        return Ok(await service.ListarDetalhesAsync(new ListarFaturasCartaoDetalheRequest(competencia, tipoTransacao), cancellationToken));
    }

    [HttpPost("{id:long}/efetivar")]
    public async Task<IActionResult> Efetivar(long id, CancellationToken cancellationToken)
    {
        return Ok(await service.EfetivarAsync(id, cancellationToken));
    }

    [HttpPost("{id:long}/estornar")]
    public async Task<IActionResult> Estornar(long id, CancellationToken cancellationToken)
    {
        return Ok(await service.EstornarAsync(id, cancellationToken));
    }
}
