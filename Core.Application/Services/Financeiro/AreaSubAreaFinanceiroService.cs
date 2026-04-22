using Core.Application.DTOs.Financeiro;
using Core.Domain.Enums.Financeiro;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed class AreaSubAreaFinanceiroService(
    IAreaRepository areaRepository,
    IUsuarioAutenticadoProvider usuarioAutenticadoProvider)
{
    public async Task<IReadOnlyCollection<AreaListaDto>> ListarAreasComSubAreasAsync(string? tipo = null, CancellationToken cancellationToken = default)
    {
        var tipoArea = ObterTipoArea(tipo);

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

    public async Task<IReadOnlyCollection<AreaRateioListaDto>> ListarAreasComSubAreasESomaRateioAsync(string? tipo = null, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");
        var tipoArea = ObterTipoArea(tipo);

        var areas = await areaRepository.ListarComSubAreasAsync(tipoArea, cancellationToken);
        var somas = await areaRepository.ListarSomaRateioPorAreaSubAreaAsync(usuarioAutenticadoId, tipoArea, cancellationToken);
        var somasPorAreaSubArea = somas.ToDictionary(x => (x.AreaId, x.SubAreaId), x => x.ValorTotalRateio);

        return areas
            .Select(area =>
            {
                var subAreas = area.SubAreas
                    .OrderBy(s => s.Nome)
                    .Select(subArea => new SubAreaRateioListaDto(
                        subArea.Id,
                        subArea.Nome,
                        somasPorAreaSubArea.GetValueOrDefault((area.Id, subArea.Id), 0m)))
                    .ToArray();

                return new AreaRateioListaDto(
                    area.Id,
                    area.Nome,
                    area.Tipo.ToString().ToLowerInvariant(),
                    subAreas.Sum(x => x.ValorTotalRateio),
                    subAreas);
            })
            .ToArray();
    }

    private static TipoAreaFinanceira? ObterTipoArea(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            return null;

        if (!Enum.TryParse<TipoAreaFinanceira>(tipo, true, out var tipoParseado))
            throw new DomainException("tipo_area_invalido");

        return tipoParseado;
    }
}
