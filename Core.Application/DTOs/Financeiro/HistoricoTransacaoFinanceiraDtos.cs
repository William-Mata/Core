using Core.Domain.Enums;

namespace Core.Application.DTOs.Financeiro;

public sealed record ListarHistoricoTransacaoFinanceiraRequest(
    int QuantidadeRegistros = 50,
    OrdemRegistrosHistoricoTransacaoFinanceira OrdemRegistros = OrdemRegistrosHistoricoTransacaoFinanceira.MaisRecentes);

public sealed record HistoricoTransacaoFinanceiraListaDto(
    long IdTransacao,
    string TipoTransacao,
    decimal Valor,
    string Descricao,
    DateOnly DataEfetivacao,
    string? TipoPagamento,
    string? ContaBancaria,
    string? Cartao,
    string? TipoDespesa,
    string? TipoReceita);

public sealed record ResumoHistoricoTransacaoFinanceiraDto(
    int? Ano,
    decimal TotalReceitas,
    decimal TotalDespesas,
    decimal TotalReembolsos,
    decimal TotalEstornos,
    decimal TotalGeral);

public sealed record ResumoHistoricoTransacaoFinanceiraMesDto(
    string Mes,
    decimal TotalReceitas,
    decimal TotalDespesas,
    decimal TotalReembolsos,
    decimal TotalEstornos);
