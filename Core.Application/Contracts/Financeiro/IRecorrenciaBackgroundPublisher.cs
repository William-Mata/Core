namespace Core.Application.Contracts.Financeiro;

public interface IRecorrenciaBackgroundPublisher
{
    Task PublicarDespesaAsync(DespesaRecorrenciaBackgroundMessage message, CancellationToken cancellationToken = default);
    Task PublicarReceitaAsync(ReceitaRecorrenciaBackgroundMessage message, CancellationToken cancellationToken = default);
}
