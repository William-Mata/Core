using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

[ApiController]
[Route("api/financeiro/historico-transacoes")]
[Authorize]
public sealed class HistoricoTransacaoFinanceiraController(HistoricoTransacaoFinanceiraConsultaService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int quantidadeRegistros = 50,
        [FromQuery] OrdemRegistrosHistoricoTransacaoFinanceira ordemRegistros = OrdemRegistrosHistoricoTransacaoFinanceira.MaisRecentes,
        CancellationToken cancellationToken = default) =>
        Ok(await service.ListarAsync(new ListarHistoricoTransacaoFinanceiraRequest(quantidadeRegistros, ordemRegistros), cancellationToken));

    [HttpGet("resumo")]
    public async Task<IActionResult> ObterResumo(
        [FromQuery] int? ano = null,
        CancellationToken cancellationToken = default) =>
        Ok(await service.ObterResumoAsync(ano, cancellationToken));

    [HttpGet("resumo-por-ano")]
    public async Task<IActionResult> ObterResumoPorAno(
        [FromQuery] int ano,
        CancellationToken cancellationToken = default) =>
        Ok(await service.ObterResumoPorAnoAsync(ano, cancellationToken));
}
