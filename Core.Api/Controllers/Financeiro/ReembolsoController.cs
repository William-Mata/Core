using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

/// <summary>
/// Endpoints de gestao de reembolsos.
/// </summary>
[ApiController]
[Route("api/financeiro/reembolsos")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public sealed class ReembolsoController(ReembolsoService service) : ControllerBase
{
    /// <summary>
    /// Lista reembolsos com filtros opcionais.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? id,
        [FromQuery] string? descricao,
        [FromQuery] string? competencia,
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim,
        [FromQuery] bool desconsiderarVinculadosCartaoCredito,
        [FromQuery] bool desconsiderarCancelados,
        CancellationToken cancellationToken) =>
        Ok(await service.ListarAsync(new ListarReembolsosRequest(id, descricao, competencia, dataInicio, dataFim, desconsiderarVinculadosCartaoCredito, desconsiderarCancelados), cancellationToken));

    /// <summary>
    /// Obtem um reembolso por identificador.
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(long id, CancellationToken cancellationToken) =>
        Ok(await service.ObterAsync(id, cancellationToken));

    /// <summary>
    /// Cria um novo reembolso.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] SalvarReembolsoRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = result.Id }, result);
    }

    /// <summary>
    /// Atualiza um reembolso existente.
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(long id, [FromBody] SalvarReembolsoRequest request, CancellationToken cancellationToken) =>
        Ok(await service.AtualizarAsync(id, request, cancellationToken));

    /// <summary>
    /// Exclui um reembolso.
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(long id, CancellationToken cancellationToken)
    {
        await service.ExcluirAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Efetiva um reembolso pendente.
    /// </summary>
    [HttpPost("{id:long}/efetivar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Efetivar(long id, [FromBody] EfetivarReembolsoRequest request, CancellationToken cancellationToken) =>
        Ok(await service.EfetivarAsync(id, request, cancellationToken));

    /// <summary>
    /// Estorna um reembolso efetivado.
    /// </summary>
    [HttpPost("{id:long}/estornar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Estornar(long id, [FromBody] EstornarReembolsoRequest request, CancellationToken cancellationToken) =>
        Ok(await service.EstornarAsync(id, request, cancellationToken));
}
