namespace Core.Application.DTOs.Financeiro;

public sealed record SubAreaListaDto(long Id, string Nome);
public sealed record AreaListaDto(long Id, string Nome, string Tipo, IReadOnlyCollection<SubAreaListaDto> SubAreas);
public sealed record AmigoListaDto(int Id, string Nome, string Email);
