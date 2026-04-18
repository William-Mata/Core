using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Financeiro;

/// <summary>
/// Endpoints de cartoes do modulo financeiro.
/// </summary>
[ApiController]
[Route("api/financeiro/cartoes")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public sealed class CartaoController(CartaoService service) : ControllerBase
{
    /// <summary>
    /// Lista os cartoes do usuario.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) => Ok(await service.ListarAsync(cancellationToken));

    /// <summary>
    /// Obtem um cartao por identificador.
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(long id, CancellationToken cancellationToken) => Ok(await service.ObterAsync(id, cancellationToken));

    /// <summary>
    /// Lista lancamentos do cartao por competencia.
    /// </summary>
    [HttpGet("{id:long}/lancamentos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarLancamentos(long id, [FromQuery] string? competencia, CancellationToken cancellationToken) =>
        Ok(await service.ListarLancamentosAsync(id, competencia, cancellationToken));

    /// <summary>
    /// Cria um novo cartao.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarCartaoRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = result.Id }, result);
    }

    /// <summary>
    /// Atualiza os dados de um cartao.
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(long id, [FromBody] AtualizarCartaoRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.AtualizarAsync(id, request, cancellationToken));
    }

    /// <summary>
    /// Inativa um cartao.
    /// </summary>
    [HttpPost("{id:long}/inativar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Inativar(long id, [FromBody] AlternarStatusCartaoRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.InativarAsync(id, request, cancellationToken));
    }

    /// <summary>
    /// Ativa um cartao.
    /// </summary>
    [HttpPost("{id:long}/ativar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ativar(long id, CancellationToken cancellationToken)
    {
        return Ok(await service.AtivarAsync(id, cancellationToken));
    }
}
