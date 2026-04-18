using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

/// <summary>
/// Endpoints de contas bancarias do modulo financeiro.
/// </summary>
[ApiController]
[Route("api/financeiro/contas-bancarias")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public sealed class ContaBancariaController(ContaBancariaService service) : ControllerBase
{
    /// <summary>
    /// Lista as contas bancarias do usuario.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) => Ok(await service.ListarAsync(cancellationToken));

    /// <summary>
    /// Obtem os detalhes de uma conta bancaria.
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(long id, CancellationToken cancellationToken) => Ok(await service.ObterAsync(id, cancellationToken));

    /// <summary>
    /// Lista lancamentos da conta por competencia.
    /// </summary>
    [HttpGet("{id:long}/lancamentos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarLancamentos(long id, [FromQuery] string? competencia, CancellationToken cancellationToken) =>
        Ok(await service.ListarLancamentosAsync(id, competencia, cancellationToken));

    /// <summary>
    /// Cria uma nova conta bancaria.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarContaBancariaRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = result.Id }, result);
    }

    /// <summary>
    /// Atualiza os dados de uma conta bancaria.
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(long id, [FromBody] AtualizarContaBancariaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.AtualizarAsync(id, request, cancellationToken));
    }

    /// <summary>
    /// Inativa uma conta bancaria.
    /// </summary>
    [HttpPost("{id:long}/inativar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Inativar(long id, [FromBody] AlternarStatusContaBancariaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.InativarAsync(id, request, cancellationToken));
    }

    /// <summary>
    /// Ativa uma conta bancaria.
    /// </summary>
    [HttpPost("{id:long}/ativar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ativar(long id, CancellationToken cancellationToken)
    {
        return Ok(await service.AtivarAsync(id, cancellationToken));
    }
}
