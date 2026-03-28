using Core.Application.DTOs;
using Core.Application.Services.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

[ApiController]
[Route("api/financeiro/reembolsos")]
[Authorize]
public sealed class ReembolsoController(ReembolsoService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? id,
        [FromQuery] string? descricao,
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim,
        CancellationToken cancellationToken) =>
        Ok(await service.ListarAsync(new ListarReembolsosRequest(id, descricao, dataInicio, dataFim), cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id, CancellationToken cancellationToken) =>
        Ok(await service.ObterAsync(id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarReembolsoRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] SalvarReembolsoRequest request, CancellationToken cancellationToken) =>
        Ok(await service.AtualizarAsync(id, request, cancellationToken));

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Excluir(long id, CancellationToken cancellationToken)
    {
        await service.ExcluirAsync(id, cancellationToken);
        return NoContent();
    }
}
