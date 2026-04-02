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

    public Task<List<HistoricoTransacaoFinanceira>> ListarPorContaBancariaCompetenciaAsync(long contaBancariaId, int usuarioOperacaoId, string? competencia, CancellationToken cancellationToken = default)
    {
        var competenciaMesAno = CompetenciaFiltroHelper.ResolverMesAno(competencia);
        var query = dbContext.HistoricosTransacoesFinanceiras
            .Where(x => x.UsuarioOperacaoId == usuarioOperacaoId)
            .Where(x => x.ContaBancariaId == contaBancariaId);

        if (competenciaMesAno.HasValue)
        {
            query = query.Where(x =>
                x.DataTransacao.Year == competenciaMesAno.Value.Ano &&
                x.DataTransacao.Month == competenciaMesAno.Value.Mes);
        }

        return query
            .OrderByDescending(x => x.DataTransacao)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<List<HistoricoTransacaoFinanceira>> ListarPorCartaoCompetenciaAsync(long cartaoId, int usuarioOperacaoId, string? competencia, CancellationToken cancellationToken = default)
    {
        var competenciaMesAno = CompetenciaFiltroHelper.ResolverMesAno(competencia);
        var query = dbContext.HistoricosTransacoesFinanceiras
            .Where(x => x.UsuarioOperacaoId == usuarioOperacaoId)
            .Where(x => x.CartaoId == cartaoId);

        if (competenciaMesAno.HasValue)
        {
            query = query.Where(x =>
                x.DataTransacao.Year == competenciaMesAno.Value.Ano &&
                x.DataTransacao.Month == competenciaMesAno.Value.Mes);
        }

        return query
            .OrderByDescending(x => x.DataTransacao)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }
}
