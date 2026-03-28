using Core.Domain.Enums;

namespace Core.Application.DTOs;

public sealed record DespesaLogDto(long Id, DateOnly Data, AcaoLogs Acao, string Descricao);
public sealed record DespesaDto(long Id, string Descricao, string? Observacao, DateOnly DataLancamento, DateOnly DataVencimento, DateOnly? DataEfetivacao, string TipoDespesa, string TipoPagamento, Recorrencia Recorrencia, decimal ValorTotal, decimal ValorLiquido, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, decimal? ValorEfetivacao, string Status, IReadOnlyCollection<string> AmigosRateio, IReadOnlyCollection<string> TiposRateio, string? AnexoDocumento, IReadOnlyCollection<DespesaLogDto> Logs);

public sealed record CriarDespesaRequest(string Descricao, string? Observacao, DateOnly DataLancamento, DateOnly DataVencimento, string TipoDespesa, string TipoPagamento, Recorrencia Recorrencia, decimal ValorTotal, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, IReadOnlyCollection<string> AmigosRateio, IReadOnlyCollection<string> TiposRateio, string? AnexoDocumento);
public sealed record AtualizarDespesaRequest(string Descricao, string? Observacao, DateOnly DataLancamento, DateOnly DataVencimento, string TipoDespesa, string TipoPagamento, Recorrencia Recorrencia, decimal ValorTotal, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, IReadOnlyCollection<string> AmigosRateio, IReadOnlyCollection<string> TiposRateio, string? AnexoDocumento);
public sealed record EfetivarDespesaRequest(DateOnly DataEfetivacao, string TipoPagamento, decimal ValorTotal, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, string? AnexoDocumento);
