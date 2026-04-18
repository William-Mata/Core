using Core.Application.Services.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

/// <summary>
/// Endpoints de consulta de areas e subareas financeiras.
/// </summary>
[ApiController]
[Route("api/financeiro/areas-subareas")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public sealed class AreaSubAreaFinanceiroController(AreaSubAreaFinanceiroService service) : ControllerBase
{
    /// <summary>
    /// Lista areas com suas respectivas subareas.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] string? tipo, CancellationToken cancellationToken) =>
        Ok(await service.ListarAreasComSubAreasAsync(tipo, cancellationToken));

    /// <summary>
    /// Lista areas/subareas incluindo total de rateio.
    /// </summary>
    [HttpGet("soma-rateio")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarComSomaRateio([FromQuery] string? tipo, CancellationToken cancellationToken) =>
        Ok(await service.ListarAreasComSubAreasESomaRateioAsync(tipo, cancellationToken));
}
