using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

/// <summary>
/// Endpoints de amizade financeira e convites.
/// </summary>
[ApiController]
[Route("api/financeiro/amigos")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public sealed class AmigoController(AmigoService service) : ControllerBase
{
    /// <summary>
    /// Lista os amigos financeiros do usuario autenticado.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) =>
        Ok(await service.ListarAmigosAsync(cancellationToken));

    /// <summary>
    /// Envia um novo convite de amizade.
    /// </summary>
    [HttpPost("convites")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> EnviarConvite([FromBody] EnviarConviteAmizadeRequest request, CancellationToken cancellationToken) =>
        Ok(await service.EnviarConviteAsync(request, cancellationToken));

    /// <summary>
    /// Lista convites recebidos e pendentes.
    /// </summary>
    [HttpGet("convites")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarConvites(CancellationToken cancellationToken) =>
        Ok(await service.ListarConvitesAsync(cancellationToken));

    /// <summary>
    /// Aceita um convite de amizade.
    /// </summary>
    [HttpPost("convites/{id:long}/aceitar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AceitarConvite(long id, CancellationToken cancellationToken) =>
        Ok(await service.AceitarConviteAsync(id, cancellationToken));

    /// <summary>
    /// Rejeita um convite de amizade.
    /// </summary>
    [HttpPost("convites/{id:long}/rejeitar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejeitarConvite(long id, CancellationToken cancellationToken) =>
        Ok(await service.RejeitarConviteAsync(id, cancellationToken));

    /// <summary>
    /// Remove uma amizade existente.
    /// </summary>
    [HttpDelete("{amigoId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoverAmizade(int amigoId, CancellationToken cancellationToken)
    {
        await service.RemoverAmizadeAsync(amigoId, cancellationToken);
        return NoContent();
    }
}
