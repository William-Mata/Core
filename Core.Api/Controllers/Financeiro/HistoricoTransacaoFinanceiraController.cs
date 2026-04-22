using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Core.Domain.Enums.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

/// <summary>
/// Endpoints de consulta do historico de transacoes financeiras.
/// </summary>
[ApiController]
[Route("api/financeiro/historico-transacoes")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public sealed class HistoricoTransacaoFinanceiraController(HistoricoTransacaoFinanceiraConsultaService service) : ControllerBase
{
    /// <summary>
    /// Lista transacoes financeiras com ordenacao e limite de registros.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] int quantidadeRegistros = 50,
        [FromQuery] OrdemRegistrosHistoricoTransacaoFinanceira ordemRegistros = OrdemRegistrosHistoricoTransacaoFinanceira.MaisRecentes,
        CancellationToken cancellationToken = default) =>
        Ok(await service.ListarAsync(new ListarHistoricoTransacaoFinanceiraRequest(quantidadeRegistros, ordemRegistros), cancellationToken));

    /// <summary>
    /// Obtem resumo de transacoes por tipo para um ano especifico ou ano atual.
    /// </summary>
    [HttpGet("resumo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterResumo(
        [FromQuery] int? ano = null,
        CancellationToken cancellationToken = default) =>
        Ok(await service.ObterResumoAsync(ano, cancellationToken));

    /// <summary>
    /// Obtem resumo anual consolidado de transacoes.
    /// </summary>
    [HttpGet("resumo-por-ano")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterResumoPorAno(
        [FromQuery] int ano,
        CancellationToken cancellationToken = default) =>
        Ok(await service.ObterResumoPorAnoAsync(ano, cancellationToken));
}
