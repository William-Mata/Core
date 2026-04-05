using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

[ApiController]
[Route("api/financeiro/amigos")]
[Authorize]
public sealed class AmigoController(AmigoService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) =>
        Ok(await service.ListarAmigosAsync(cancellationToken));

    [HttpPost("convites")]
    public async Task<IActionResult> EnviarConvite([FromBody] EnviarConviteAmizadeRequest request, CancellationToken cancellationToken) =>
        Ok(await service.EnviarConviteAsync(request, cancellationToken));

    [HttpGet("convites")]
    public async Task<IActionResult> ListarConvites(CancellationToken cancellationToken) =>
        Ok(await service.ListarConvitesAsync(cancellationToken));

    [HttpPost("convites/{id:long}/aceitar")]
    public async Task<IActionResult> AceitarConvite(long id, CancellationToken cancellationToken) =>
        Ok(await service.AceitarConviteAsync(id, cancellationToken));

    [HttpPost("convites/{id:long}/rejeitar")]
    public async Task<IActionResult> RejeitarConvite(long id, CancellationToken cancellationToken) =>
        Ok(await service.RejeitarConviteAsync(id, cancellationToken));

    [HttpDelete("{amigoId:int}")]
    public async Task<IActionResult> RemoverAmizade(int amigoId, CancellationToken cancellationToken)
    {
        await service.RemoverAmizadeAsync(amigoId, cancellationToken);
        return NoContent();
    }
}
