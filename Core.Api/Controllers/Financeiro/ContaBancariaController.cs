using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

[ApiController]
[Route("api/financeiro/contas-bancarias")]
[Authorize]
public sealed class ContaBancariaController(ContaBancariaService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) => Ok(await service.ListarAsync(cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id, CancellationToken cancellationToken) => Ok(await service.ObterAsync(id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarContaBancariaRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] AtualizarContaBancariaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.AtualizarAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:long}/inativar")]
    public async Task<IActionResult> Inativar(long id, [FromBody] AlternarStatusContaBancariaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.InativarAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:long}/ativar")]
    public async Task<IActionResult> Ativar(long id, CancellationToken cancellationToken)
    {
        return Ok(await service.AtivarAsync(id, cancellationToken));
    }
}
