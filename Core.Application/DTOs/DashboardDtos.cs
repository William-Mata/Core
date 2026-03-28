namespace Core.Application.DTOs;

public sealed record TransacaoDashboardDto(long Id, string Tipo, decimal Valor, string Descricao, DateOnly DataEfetivacao, string CodigoPagamento, string TipoPagamento, string? ContaBancaria, string? Cartao, string Area, string Subarea);
public sealed record ItemBalancoDashboardDto(string Id, string Tipo, string Nome, string Subtitulo, decimal Saldo);
public sealed record DashboardDto(IReadOnlyCollection<TransacaoDashboardDto> Transacoes, IReadOnlyCollection<ItemBalancoDashboardDto> Balanco);
