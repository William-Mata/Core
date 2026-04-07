using Core.Domain.Enums;

namespace Core.Application.DTOs.Financeiro;

public sealed record SubAreaListaDto(long Id, string Nome);
public sealed record AreaListaDto(long Id, string Nome, string Tipo, IReadOnlyCollection<SubAreaListaDto> SubAreas);
public sealed record AmigoListaDto(int Id, string Nome, string Email);
public sealed record LancamentoVinculadoDto(
    long Id,
    long TransacaoId,
    DateOnly DataTransacao,
    string TipoTransacao,
    string TipoOperacao,
    string Descricao,
    TipoPagamento? TipoPagamento,
    decimal ValorAntesTransacao,
    decimal ValorTransacao,
    decimal ValorDepoisTransacao);
