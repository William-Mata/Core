using Core.Application.DTOs.Compras;
using Core.Application.Services.Compras;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Compras;

/// <summary>
/// Endpoints de desejos de compra e conversao em listas.
/// </summary>
[ApiController]
[Route("api/compras/desejos")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public sealed class DesejoCompraController(ComprasService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) =>
        Ok(await service.ListarDesejosAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Criar([FromBody] CriarDesejoCompraRequest request, CancellationToken cancellationToken) =>
        Ok(await service.CriarDesejoAsync(request, cancellationToken));

    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(long id, [FromBody] AtualizarDesejoCompraRequest request, CancellationToken cancellationToken) =>
        Ok(await service.AtualizarDesejoAsync(id, request, cancellationToken));

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(long id, CancellationToken cancellationToken)
    {
        await service.ExcluirDesejoAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("converter")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Converter([FromBody] ConverterDesejosCompraRequest request, CancellationToken cancellationToken) =>
        Ok(await service.ConverterDesejosAsync(request, cancellationToken));
}
