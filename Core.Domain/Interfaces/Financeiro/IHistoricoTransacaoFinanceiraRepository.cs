using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;

namespace Core.Domain.Interfaces.Financeiro;

public interface IHistoricoTransacaoFinanceiraRepository
{
    Task<HistoricoTransacaoFinanceira> CriarAsync(HistoricoTransacaoFinanceira historico, CancellationToken cancellationToken = default);
    Task<HistoricoTransacaoFinanceira?> ObterUltimoPorTransacaoAsync(TipoTransacaoFinanceira tipoTransacao, long transacaoId, CancellationToken cancellationToken = default);
}
