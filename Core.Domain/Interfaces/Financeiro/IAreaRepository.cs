using Core.Domain.Entities.Financeiro;

namespace Core.Domain.Interfaces.Financeiro;

public interface IAreaRepository
{
    Task<List<SubArea>> ObterSubAreasPorIdsAsync(IReadOnlyCollection<long> subAreasIds, CancellationToken cancellationToken = default);
}

