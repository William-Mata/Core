using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class AreaRepository(AppDbContext dbContext) : IAreaRepository
{
    public Task<List<Area>> ListarComSubAreasAsync(TipoAreaFinanceira? tipo = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Areas
            .Include(x => x.SubAreas)
            .AsQueryable();

        if (tipo.HasValue)
            query = query.Where(x => x.Tipo == tipo.Value);

        return query
            .OrderBy(x => x.Tipo)
            .ThenBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<List<SubArea>> ObterSubAreasPorIdsAsync(IReadOnlyCollection<long> subAreasIds, CancellationToken cancellationToken = default)
    {
        if (subAreasIds.Count == 0) return Task.FromResult(new List<SubArea>());

        return dbContext.SubAreas
            .Include(x => x.Area)
            .Where(x => subAreasIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AreaSubAreaRateioSoma>> ListarSomaRateioPorAreaSubAreaAsync(int usuarioId, TipoAreaFinanceira? tipo = null, CancellationToken cancellationToken = default)
    {
        if (usuarioId <= 0)
            return [];

        var somasDespesa = tipo is null or TipoAreaFinanceira.Despesa
            ? await (
                from rateio in dbContext.DespesasAreasRateio.AsNoTracking()
                join despesa in dbContext.Despesas.AsNoTracking() on rateio.DespesaId equals despesa.Id
                where rateio.UsuarioCadastroId == usuarioId
                    && rateio.Valor.HasValue
                    && despesa.Status == StatusDespesa.Efetivada
                select rateio)
                .GroupBy(x => new { x.AreaId, x.SubAreaId })
                .Select(x => new AreaSubAreaRateioSoma(x.Key.AreaId, x.Key.SubAreaId, x.Sum(y => y.Valor ?? 0m)))
                .ToListAsync(cancellationToken)
            : [];

        var somasReceita = tipo is null or TipoAreaFinanceira.Receita
            ? await (
                from rateio in dbContext.ReceitasAreasRateio.AsNoTracking()
                join receita in dbContext.Receitas.AsNoTracking() on rateio.ReceitaId equals receita.Id
                where rateio.UsuarioCadastroId == usuarioId
                    && rateio.Valor.HasValue
                    && receita.Status == StatusReceita.Efetivada
                select rateio)
                .GroupBy(x => new { x.AreaId, x.SubAreaId })
                .Select(x => new AreaSubAreaRateioSoma(x.Key.AreaId, x.Key.SubAreaId, x.Sum(y => y.Valor ?? 0m)))
                .ToListAsync(cancellationToken)
            : [];

        return somasDespesa
            .Concat(somasReceita)
            .GroupBy(x => new { x.AreaId, x.SubAreaId })
            .Select(x => new AreaSubAreaRateioSoma(x.Key.AreaId, x.Key.SubAreaId, x.Sum(y => y.ValorTotalRateio)))
            .ToList();
    }
}
