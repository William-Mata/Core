using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;

namespace Core.Domain.Interfaces.Financeiro;

public interface IAreaRepository
{
    Task<List<SubArea>> ObterSubAreasPorIdsAsync(IReadOnlyCollection<long> subAreasIds, CancellationToken cancellationToken = default);
    Task<List<Area>> ListarComSubAreasAsync(TipoAreaFinanceira? tipo = null, CancellationToken cancellationToken = default);
}
