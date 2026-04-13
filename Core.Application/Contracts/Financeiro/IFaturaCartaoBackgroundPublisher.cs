namespace Core.Application.Contracts.Financeiro;

public interface IFaturaCartaoBackgroundPublisher
{
    Task PublicarGarantiaESaneamentoAsync(FaturaCartaoGarantiaSaneamentoBackgroundMessage message, CancellationToken cancellationToken = default);
}

