using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

[ApiController]
[Route("api/financeiro/cartoes")]
[Authorize]
public sealed class CartaoController(CartaoService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) => Ok(await service.ListarAsync(cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id, CancellationToken cancellationToken) => Ok(await service.ObterAsync(id, cancellationToken));

    [HttpGet("{id:long}/lancamentos")]
    public async Task<IActionResult> ListarLancamentos(long id, [FromQuery] string? competencia, CancellationToken cancellationToken) =>
        Ok(await service.ListarLancamentosAsync(id, competencia, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarCartaoRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] AtualizarCartaoRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.AtualizarAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:long}/inativar")]
    public async Task<IActionResult> Inativar(long id, [FromBody] AlternarStatusCartaoRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.InativarAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:long}/ativar")]
    public async Task<IActionResult> Ativar(long id, CancellationToken cancellationToken)
    {
        return Ok(await service.AtivarAsync(id, cancellationToken));
    }
}
