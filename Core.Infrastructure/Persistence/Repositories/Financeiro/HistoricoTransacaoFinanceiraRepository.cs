using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums.Financeiro;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class HistoricoTransacaoFinanceiraRepository(AppDbContext dbContext) : IHistoricoTransacaoFinanceiraRepository
{
    public async Task<HistoricoTransacaoFinanceira> CriarAsync(HistoricoTransacaoFinanceira historico, CancellationToken cancellationToken = default)
    {
        var valorImpacto = ResolverValorImpacto(historico.TipoTransacao, historico.TipoOperacao, historico.ValorTransacao);

        if (historico.ContaBancariaId.HasValue)
        {
            var conta = await dbContext.ContasBancarias
                .FirstOrDefaultAsync(x => x.Id == historico.ContaBancariaId.Value, cancellationToken);

            if (conta is not null)
            {
                var saldoAntes = conta.SaldoAtual;
                var saldoDepois = saldoAntes + valorImpacto;
                conta.SaldoAtual = saldoDepois;
                historico.ValorAntesTransacao = saldoAntes;
                historico.ValorDepoisTransacao = saldoDepois;
            }
        }
        else if (historico.CartaoId.HasValue)
        {
            var cartao = await dbContext.Cartoes
                .FirstOrDefaultAsync(x => x.Id == historico.CartaoId.Value, cancellationToken);

            if (cartao is not null)
            {
                var saldoAntes = cartao.SaldoDisponivel;
                var saldoDepois = saldoAntes + valorImpacto;
                cartao.SaldoDisponivel = saldoDepois;
                historico.ValorAntesTransacao = saldoAntes;
                historico.ValorDepoisTransacao = saldoDepois;
            }
        }

        historico.ValorTransacao = valorImpacto;
        dbContext.HistoricosTransacoesFinanceiras.Add(historico);
        await dbContext.SaveChangesAsync(cancellationToken);
        return historico;
    }

    public Task<HistoricoTransacaoFinanceira?> ObterUltimoPorTransacaoAsync(TipoTransacaoFinanceira tipoTransacao, long transacaoId, CancellationToken cancellationToken = default) =>
        dbContext.HistoricosTransacoesFinanceiras
            .Where(x => x.TipoTransacao == tipoTransacao && x.TransacaoId == transacaoId)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task MarcarOcultoPorTransacaoAsync(TipoTransacaoFinanceira tipoTransacao, long transacaoId, CancellationToken cancellationToken = default)
    {
        var historicos = await dbContext.HistoricosTransacoesFinanceiras
            .Where(x => x.TipoTransacao == tipoTransacao && x.TransacaoId == transacaoId)
            .Where(x => !x.OcultarDoHistorico)
            .ToListAsync(cancellationToken);

        if (historicos.Count == 0)
            return;

        foreach (var historico in historicos)
            historico.OcultarDoHistorico = true;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<HistoricoTransacaoFinanceira>> ListarPorUsuarioAsync(int usuarioOperacaoId, int quantidadeRegistros, OrdemRegistrosHistoricoTransacaoFinanceira ordemRegistros, CancellationToken cancellationToken = default)
    {
        var query = dbContext.HistoricosTransacoesFinanceiras
            .Where(x => x.UsuarioOperacaoId == usuarioOperacaoId)
            .Where(x => !x.OcultarDoHistorico)
            .AsQueryable();

        query = ordemRegistros switch
        {
            OrdemRegistrosHistoricoTransacaoFinanceira.MaisAntigos => query
                .OrderBy(x => x.DataTransacao)
                .ThenBy(x => x.Id),
            _ => query
                .OrderByDescending(x => x.DataTransacao)
                .ThenByDescending(x => x.Id)
        };

        return query
            .Take(quantidadeRegistros)
            .ToListAsync(cancellationToken);
    }

    public Task<List<HistoricoTransacaoFinanceira>> ListarPorUsuarioResumoAsync(int usuarioOperacaoId, int? ano, CancellationToken cancellationToken = default) =>
        dbContext.HistoricosTransacoesFinanceiras
            .Where(x => x.UsuarioOperacaoId == usuarioOperacaoId)
            .Where(x => !x.OcultarDoHistorico)
            .Where(x => !ano.HasValue || x.DataTransacao.Year == ano.Value)
            .ToListAsync(cancellationToken);

    public Task<List<HistoricoTransacaoFinanceira>> ListarPorContaBancariaCompetenciaAsync(long contaBancariaId, int usuarioOperacaoId, string? competencia, CancellationToken cancellationToken = default)
    {
        var competenciaMesAno = CompetenciaFiltroHelper.ResolverMesAno(competencia);
        var query = dbContext.HistoricosTransacoesFinanceiras
            .Where(x => x.UsuarioOperacaoId == usuarioOperacaoId)
            .Where(x => !x.OcultarDoHistorico)
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
            .Where(x => !x.OcultarDoHistorico)
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

    private static decimal ResolverValorImpacto(TipoTransacaoFinanceira tipoTransacao, TipoOperacaoTransacaoFinanceira tipoOperacao, decimal valorTransacao)
    {
        var valorAbsoluto = Math.Abs(valorTransacao);

        return (tipoTransacao, tipoOperacao) switch
        {
            (TipoTransacaoFinanceira.Despesa, TipoOperacaoTransacaoFinanceira.Efetivacao) => -valorAbsoluto,
            (TipoTransacaoFinanceira.Despesa, TipoOperacaoTransacaoFinanceira.Estorno) => valorAbsoluto,
            (TipoTransacaoFinanceira.Receita, TipoOperacaoTransacaoFinanceira.Efetivacao) => valorAbsoluto,
            (TipoTransacaoFinanceira.Receita, TipoOperacaoTransacaoFinanceira.Estorno) => -valorAbsoluto,
            (TipoTransacaoFinanceira.Reembolso, TipoOperacaoTransacaoFinanceira.Efetivacao) => valorAbsoluto,
            (TipoTransacaoFinanceira.Reembolso, TipoOperacaoTransacaoFinanceira.Estorno) => -valorAbsoluto,
            _ => valorTransacao
        };
    }
}
