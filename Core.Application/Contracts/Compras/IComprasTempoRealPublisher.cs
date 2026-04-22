namespace Core.Application.Contracts.Compras;

public interface IComprasTempoRealPublisher
{
    Task PublicarAtualizacaoListaAsync(
        long listaId,
        string evento,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
