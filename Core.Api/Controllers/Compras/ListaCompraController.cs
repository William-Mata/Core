using Core.Application.DTOs.Compras;
using Core.Application.Services.Compras;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers.Compras;

/// <summary>
/// Endpoints de listas de compra, itens, compartilhamento e logs.
/// </summary>
[ApiController]
[Route("api/compras/listas")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public sealed class ListaCompraController(ComprasService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] bool incluirArquivadas = false, CancellationToken cancellationToken = default) =>
        Ok(await service.ListarListasAsync(incluirArquivadas, cancellationToken));

    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(long id, CancellationToken cancellationToken) =>
        Ok(await service.ObterListaAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarListaCompraRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CriarListaAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(long id, [FromBody] AtualizarListaCompraRequest request, CancellationToken cancellationToken) =>
        Ok(await service.AtualizarListaAsync(id, request, cancellationToken));

    [HttpPost("{id:long}/arquivar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Arquivar(long id, CancellationToken cancellationToken) =>
        Ok(await service.ArquivarListaAsync(id, cancellationToken));

    [HttpPost("{id:long}/duplicar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Duplicar(long id, [FromBody] CriarListaCompraRequest request, CancellationToken cancellationToken) =>
        Ok(await service.DuplicarListaAsync(id, request, cancellationToken));

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(long id, CancellationToken cancellationToken)
    {
        await service.ExcluirListaAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/participantes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Compartilhar(long id, [FromBody] CompartilharListaCompraRequest request, CancellationToken cancellationToken) =>
        Ok(await service.CompartilharListaAsync(id, request, cancellationToken));

    [HttpDelete("{id:long}/participantes/{participanteId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoverParticipante(long id, int participanteId, CancellationToken cancellationToken)
    {
        await service.RemoverParticipanteAsync(id, participanteId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:long}/sugestoes-itens")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> BuscarSugestoesItens(long id, [FromQuery] string? descricao, CancellationToken cancellationToken) =>
        Ok(await service.BuscarSugestoesItensAsync(id, descricao, cancellationToken));

    [HttpPost("{id:long}/itens")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CriarItem(long id, [FromBody] CriarItemListaCompraRequest request, CancellationToken cancellationToken) =>
        Ok(await service.CriarItemAsync(id, request, cancellationToken));

    [HttpPut("{id:long}/itens/{itemId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarItem(long id, long itemId, [FromBody] AtualizarItemListaCompraRequest request, CancellationToken cancellationToken) =>
        Ok(await service.AtualizarItemAsync(id, itemId, request, cancellationToken));

    [HttpPatch("{id:long}/itens/{itemId:long}/edicao-rapida")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EdicaoRapidaItem(long id, long itemId, [FromBody] EdicaoRapidaItemListaCompraRequest request, CancellationToken cancellationToken) =>
        Ok(await service.AtualizarItemEdicaoRapidaAsync(id, itemId, request, cancellationToken));

    [HttpPost("{id:long}/itens/{itemId:long}/marcar-comprado")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarItemComprado(long id, long itemId, [FromBody] MarcarCompradoItemListaCompraRequest request, CancellationToken cancellationToken) =>
        Ok(await service.MarcarItemCompradoAsync(id, itemId, request, cancellationToken));

    [HttpPost("{id:long}/acoes-lote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecutarAcaoLote(long id, [FromBody] AcaoLoteListaCompraRequest request, CancellationToken cancellationToken) =>
        Ok(await service.ExecutarAcaoLoteAsync(id, request, cancellationToken));

    [HttpGet("{id:long}/logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarLogs(long id, CancellationToken cancellationToken) =>
        Ok(await service.ListarLogsAsync(id, cancellationToken));
}
