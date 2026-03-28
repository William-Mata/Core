using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class HistoricoTransacaoFinanceiraRepository(AppDbContext dbContext) : IHistoricoTransacaoFinanceiraRepository
{
    public async Task<HistoricoTransacaoFinanceira> CriarAsync(HistoricoTransacaoFinanceira historico, CancellationToken cancellationToken = default)
    {
        dbContext.HistoricosTransacoesFinanceiras.Add(historico);
        await dbContext.SaveChangesAsync(cancellationToken);
        return historico;
    }

    public Task<HistoricoTransacaoFinanceira?> ObterUltimoPorTransacaoAsync(TipoTransacaoFinanceira tipoTransacao, long transacaoId, CancellationToken cancellationToken = default) =>
        dbContext.HistoricosTransacoesFinanceiras
            .Where(x => x.TipoTransacao == tipoTransacao && x.TransacaoId == transacaoId)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
