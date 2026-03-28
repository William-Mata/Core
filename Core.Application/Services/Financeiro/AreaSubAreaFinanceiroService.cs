using Core.Application.DTOs.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed class AreaSubAreaFinanceiroService(IAreaRepository areaRepository)
{
    public async Task<IReadOnlyCollection<AreaListaDto>> ListarAreasComSubAreasAsync(string? tipo = null, CancellationToken cancellationToken = default)
    {
        TipoAreaFinanceira? tipoArea = null;
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            if (!Enum.TryParse<TipoAreaFinanceira>(tipo, true, out var tipoParseado))
                throw new DomainException("tipo_area_invalido");

            tipoArea = tipoParseado;
        }

        var areas = await areaRepository.ListarComSubAreasAsync(tipoArea, cancellationToken);
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
