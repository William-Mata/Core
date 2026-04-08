using Core.Domain.Enums;

namespace Core.Application.DTOs.Financeiro;

public sealed record ListarHistoricoTransacaoFinanceiraRequest(
    int QuantidadeRegistros = 50,
    OrdemRegistrosHistoricoTransacaoFinanceira OrdemRegistros = OrdemRegistrosHistoricoTransacaoFinanceira.MaisRecentes);

public sealed record HistoricoTransacaoFinanceiraListaDto(
    string IdOrigem,
    string TipoTransacao,
    decimal Valor,
    string Descricao,
    DateOnly DataEfetivacao,
    string? TipoPagamento,
    string? ContaBancaria,
    string? Cartao,
    string? TipoDespesa,
    string? TipoReceita);
