using Core.Application.Services.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

[ApiController]
[Route("api/financeiro/aprovacoes/receitas")]
[Authorize]
public sealed class AprovacaoReceitaController(ReceitaService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListarPendentes(CancellationToken cancellationToken) =>
        Ok(await service.ListarPendentesAprovacaoAsync(cancellationToken));

    [HttpPost("{id:long}/aprovar")]
    public async Task<IActionResult> Aprovar(long id, CancellationToken cancellationToken) =>
        Ok(await service.AprovarRateioAsync(id, cancellationToken));

    [HttpPost("{id:long}/rejeitar")]
    public async Task<IActionResult> Rejeitar(long id, CancellationToken cancellationToken) =>
        Ok(await service.RejeitarRateioAsync(id, cancellationToken));
}
