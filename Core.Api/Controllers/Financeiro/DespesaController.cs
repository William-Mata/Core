using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

/// <summary>
/// Endpoints de gestao de despesas.
/// </summary>
[ApiController]
[Route("api/financeiro/despesas")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public sealed class DespesaController(DespesaService service) : ControllerBase
{
    /// <summary>
    /// Lista despesas com filtros opcionais.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? id,
        [FromQuery] string? descricao,
        [FromQuery] string? competencia,
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim,
        [FromQuery] bool verificarUltimaRecorrencia,
        [FromQuery] bool desconsiderarVinculadosCartaoCredito,
        [FromQuery] bool desconsiderarCancelados,
        CancellationToken cancellationToken) =>
        Ok(await service.ListarAsync(new ListarDespesasRequest(id, descricao, competencia, dataInicio, dataFim, verificarUltimaRecorrencia, desconsiderarVinculadosCartaoCredito, desconsiderarCancelados), cancellationToken));

    /// <summary>
    /// Obtem uma despesa por identificador.
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(long id, CancellationToken cancellationToken) => Ok(await service.ObterAsync(id, cancellationToken));

    /// <summary>
    /// Cria uma nova despesa.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarDespesaRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = result.Id }, result);
    }

    /// <summary>
    /// Atualiza uma despesa existente.
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(
        long id,
        [FromBody] AtualizarDespesaRequest request,
        [FromQuery] EscopoRecorrencia? escopoRecorrencia,
        CancellationToken cancellationToken)
    {
        return Ok(await service.AtualizarAsync(id, request, escopoRecorrencia ?? EscopoRecorrencia.ApenasEssa, cancellationToken));
    }

    /// <summary>
    /// Efetiva uma despesa pendente.
    /// </summary>
    [HttpPost("{id:long}/efetivar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Efetivar(long id, [FromBody] EfetivarDespesaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.EfetivarAsync(id, request, cancellationToken));
    }

    /// <summary>
    /// Cancela uma despesa com escopo opcional para recorrencias.
    /// </summary>
    [HttpPost("{id:long}/cancelar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancelar(long id, [FromQuery] EscopoRecorrencia? escopoRecorrencia, CancellationToken cancellationToken)
    {
        return Ok(await service.CancelarAsync(id, escopoRecorrencia ?? EscopoRecorrencia.ApenasEssa, cancellationToken));
    }

    /// <summary>
    /// Estorna uma despesa efetivada.
    /// </summary>
    [HttpPost("{id:long}/estornar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Estornar(long id, [FromBody] EstornarDespesaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.EstornarAsync(id, request, cancellationToken));
    }

    /// <summary>
    /// Lista despesas pendentes de aprovacao de rateio.
    /// </summary>
    [HttpGet("pendentes-aprovacao")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarPendentesAprovacao(CancellationToken cancellationToken) =>
        Ok(await service.ListarPendentesAprovacaoAsync(cancellationToken));

    /// <summary>
    /// Aprova o rateio de uma despesa.
    /// </summary>
    [HttpPost("{id:long}/aprovar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AprovarRateio(long id, CancellationToken cancellationToken) =>
        Ok(await service.AprovarRateioAsync(id, cancellationToken));

    /// <summary>
    /// Rejeita o rateio de uma despesa.
    /// </summary>
    [HttpPost("{id:long}/rejeitar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejeitarRateio(long id, CancellationToken cancellationToken) =>
        Ok(await service.RejeitarRateioAsync(id, cancellationToken));
}
