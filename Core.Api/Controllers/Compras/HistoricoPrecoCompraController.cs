using Core.Application.Services.Compras;
using Core.Domain.Enums.Compras;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Compras;

/// <summary>
/// Endpoints para consulta de historico de precos dos produtos.
/// </summary>
[ApiController]
[Route("api/compras/historico-precos")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public sealed class HistoricoPrecoCompraController(ComprasService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? descricao,
        [FromQuery] UnidadeMedidaCompra? unidade,
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        CancellationToken cancellationToken) =>
        Ok(await service.ListarHistoricoPrecosAsync(descricao, unidade, dataInicio, dataFim, cancellationToken));
}
