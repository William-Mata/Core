using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

[ApiController]
[Route("api/financeiro/receitas")]
[Authorize]
public sealed class ReceitaController(ReceitaService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? id,
        [FromQuery] string? descricao,
        [FromQuery] string? competencia,
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim,
        CancellationToken cancellationToken) =>
        Ok(await service.ListarAsync(new ListarReceitasRequest(id, descricao, competencia, dataInicio, dataFim), cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id, CancellationToken cancellationToken) => Ok(await service.ObterAsync(id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarReceitaRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] AtualizarReceitaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.AtualizarAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:long}/efetivar")]
    public async Task<IActionResult> Efetivar(long id, [FromBody] EfetivarReceitaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.EfetivarAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:long}/cancelar")]
    public async Task<IActionResult> Cancelar(long id, CancellationToken cancellationToken)
    {
        return Ok(await service.CancelarAsync(id, cancellationToken));
    }

    [HttpPost("{id:long}/estornar")]
    public async Task<IActionResult> Estornar(long id, CancellationToken cancellationToken)
    {
        return Ok(await service.EstornarAsync(id, cancellationToken));
    }
}
