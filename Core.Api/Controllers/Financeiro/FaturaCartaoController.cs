using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

/// <summary>
/// Endpoints de consulta e controle de faturas de cartao.
/// </summary>
[ApiController]
[Route("api/financeiro/faturas-cartao")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public sealed class FaturaCartaoController(FaturaCartaoService service) : ControllerBase
{
    /// <summary>
    /// Lista faturas por cartao e competencia.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] long? cartaoId,
        [FromQuery] string competencia,
        CancellationToken cancellationToken)
    {
        return Ok(await service.ListarAsync(new ListarFaturasCartaoRequest(cartaoId, competencia), cancellationToken));
    }

    /// <summary>
    /// Lista detalhes de faturas por competencia.
    /// </summary>
    [HttpGet("detalhes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarDetalhes(
        [FromQuery] string competencia,
        [FromQuery] string? tipoTransacao,
        CancellationToken cancellationToken)
    {
        return Ok(await service.ListarDetalhesAsync(new ListarFaturasCartaoDetalheRequest(competencia, tipoTransacao), cancellationToken));
    }

    /// <summary>
    /// Efetiva uma fatura.
    /// </summary>
    [HttpPost("{id:long}/efetivar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Efetivar(long id, [FromBody] EfetivarFaturaCartaoRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.EfetivarAsync(id, request, cancellationToken));
    }

    /// <summary>
    /// Estorna uma fatura efetivada.
    /// </summary>
    [HttpPost("{id:long}/estornar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Estornar(long id, [FromBody] EstornarFaturaCartaoRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.EstornarAsync(id, request, cancellationToken));
    }
}
