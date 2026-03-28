using Core.Application.DTOs.Financeiro;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed class AreaSubAreaFinanceiroService(IAreaRepository areaRepository)
{
    public async Task<IReadOnlyCollection<AreaListaDto>> ListarAreasComSubAreasAsync(CancellationToken cancellationToken = default)
    {
        var areas = await areaRepository.ListarComSubAreasAsync(cancellationToken);
        return areas
            .Select(x => new AreaListaDto(
                x.Id,
                x.Nome,
                x.Tipo.ToString().ToLowerInvariant(),
                x.SubAreas
                    .OrderBy(s => s.Nome)
                    .Select(s => new SubAreaListaDto(s.Id, s.Nome))
                    .ToArray()))
            .ToArray();
    }
}
