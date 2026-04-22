namespace Core.Application.Contracts.Compras;

public sealed record ListaCompraAtualizadaMessage(
    long ListaId,
    string Evento,
    int UsuarioId,
    DateTime DataHoraUtc);

public static class ComprasTempoRealGrupos
{
    public static string Lista(long listaId) => $"compras_lista_{listaId}";
}
