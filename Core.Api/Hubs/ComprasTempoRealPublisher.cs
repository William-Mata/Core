using Core.Application.Contracts.Compras;
using Microsoft.AspNetCore.SignalR;

namespace Core.Api.Hubs;

public sealed class ComprasTempoRealPublisher(
    IHubContext<ComprasHub> hubContext,
    ILogger<ComprasTempoRealPublisher> logger) : IComprasTempoRealPublisher
{
    public async Task PublicarAtualizacaoListaAsync(
        long listaId,
        string evento,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mensagem = new ListaCompraAtualizadaMessage(
                ListaId: listaId,
                Evento: evento,
                UsuarioId: usuarioId,
                DataHoraUtc: DateTime.UtcNow);

            await hubContext.Clients
                .Group(ComprasTempoRealGrupos.Lista(listaId))
                .SendAsync("listaAtualizada", mensagem, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Publicacao de evento em tempo real de Compras cancelada para lista {ListaId}.", listaId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao publicar evento em tempo real de Compras para lista {ListaId}.", listaId);
        }
    }
}
