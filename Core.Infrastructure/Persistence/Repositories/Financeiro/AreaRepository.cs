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
}
