using Core.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(DashboardService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obter(CancellationToken cancellationToken) => Ok(await service.ObterAsync(cancellationToken));
}
