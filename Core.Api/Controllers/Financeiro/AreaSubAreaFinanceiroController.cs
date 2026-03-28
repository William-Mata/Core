using Core.Application.Services.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

[ApiController]
[Route("api/financeiro/areas-subareas")]
[Authorize]
public sealed class AreaSubAreaFinanceiroController(AreaSubAreaFinanceiroService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? tipo, CancellationToken cancellationToken) =>
        Ok(await service.ListarAreasComSubAreasAsync(tipo, cancellationToken));
}
