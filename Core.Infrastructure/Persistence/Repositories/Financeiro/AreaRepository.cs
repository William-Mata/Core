using Core.Domain.Entities.Financeiro;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class AreaRepository(AppDbContext dbContext) : IAreaRepository
{
    public Task<List<SubArea>> ObterSubAreasPorIdsAsync(IReadOnlyCollection<long> subAreasIds, CancellationToken cancellationToken = default)
    {
        if (subAreasIds.Count == 0) return Task.FromResult(new List<SubArea>());

        return dbContext.SubAreas
            .Include(x => x.Area)
            .Where(x => subAreasIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }
}

