using System.Security.Claims;
using Core.Application.Contracts.Compras;
using Core.Domain.Interfaces.Compras;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Core.Api.Hubs;

[Authorize]
public sealed class ComprasHub(IComprasRepository comprasRepository) : Hub
{
    public async Task EntrarLista(long listaId)
    {
        var cancellationToken = Context.ConnectionAborted;
        var usuarioId = ObterUsuarioId();
        var lista = await comprasRepository.ObterListaAcessivelPorIdAsync(listaId, usuarioId, cancellationToken);
        if (lista is null)
            throw new HubException("lista_compra_sem_permissao_visualizacao");

        await Groups.AddToGroupAsync(Context.ConnectionId, ComprasTempoRealGrupos.Lista(listaId), cancellationToken);
    }

    public Task SairLista(long listaId)
    {
        var cancellationToken = Context.ConnectionAborted;
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, ComprasTempoRealGrupos.Lista(listaId), cancellationToken);
    }

    private int ObterUsuarioId()
    {
        var claimId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub")
            ?? Context.User?.FindFirstValue("usuario_id");

        if (int.TryParse(claimId, out var usuarioId))
            return usuarioId;

        throw new HubException("usuario_nao_autenticado");
    }
}
