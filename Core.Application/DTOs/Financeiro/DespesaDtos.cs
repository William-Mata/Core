using Core.Domain.Enums;

namespace Core.Application.DTOs.Financeiro;

public sealed record DespesaLogDto(long Id, DateOnly Data, AcaoLogs Acao, string Descricao);
public sealed record DespesaAreaRateioDto(long AreaId, string AreaNome, long SubAreaId, string SubAreaNome, decimal? Valor);
public sealed record AmigoRateioDto(string Nome, decimal? Valor);
public sealed record AmigoRateioRequest(string Nome, decimal? Valor);
public sealed record DespesaDto(long Id, string Descricao, string? Observacao, DateOnly DataLancamento, DateOnly DataVencimento, DateOnly? DataEfetivacao, string TipoDespesa, string TipoPagamento, Recorrencia Recorrencia, int? QuantidadeRecorrencia, bool RecorrenciaFixa, decimal ValorTotal, decimal ValorLiquido, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, decimal? ValorEfetivacao, string Status, IReadOnlyCollection<AmigoRateioDto> Amigos, IReadOnlyCollection<string> AmigosRateio, IReadOnlyDictionary<string, decimal> RateioAmigosValores, IReadOnlyCollection<DespesaAreaRateioDto> AreasRateio, IReadOnlyCollection<string> TiposRateio, string? AnexoDocumento, IReadOnlyCollection<DespesaLogDto> Logs);

public sealed record DespesaAreaRateioRequest(long AreaId, long SubAreaId, decimal? Valor);
public sealed record CriarDespesaRequest(string Descricao, string? Observacao, DateOnly DataLancamento, DateOnly DataVencimento, string TipoDespesa, string TipoPagamento, Recorrencia Recorrencia, decimal ValorTotal, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, IReadOnlyCollection<string> AmigosRateio, IReadOnlyCollection<string> TiposRateio, string? AnexoDocumento, IReadOnlyDictionary<string, decimal>? RateioAmigosValores = null, IReadOnlyCollection<DespesaAreaRateioRequest>? AreasRateio = null, int? QuantidadeRecorrencia = null, IReadOnlyCollection<AmigoRateioRequest>? Amigos = null, int? QuantidadeParcelas = null, bool RecorrenciaFixa = false);
public sealed record AtualizarDespesaRequest(string Descricao, string? Observacao, DateOnly DataLancamento, DateOnly DataVencimento, string TipoDespesa, string TipoPagamento, Recorrencia Recorrencia, decimal ValorTotal, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, IReadOnlyCollection<string> AmigosRateio, IReadOnlyCollection<string> TiposRateio, string? AnexoDocumento, IReadOnlyDictionary<string, decimal>? RateioAmigosValores = null, IReadOnlyCollection<DespesaAreaRateioRequest>? AreasRateio = null, int? QuantidadeRecorrencia = null, IReadOnlyCollection<AmigoRateioRequest>? Amigos = null, int? QuantidadeParcelas = null, bool RecorrenciaFixa = false);
public sealed record EfetivarDespesaRequest(
    DateOnly DataEfetivacao,
    string TipoPagamento,
    decimal ValorTotal,
    decimal Desconto,
    decimal Acrescimo,
    decimal Imposto,
    decimal Juros,
    string? AnexoDocumento,
    long? ContaBancariaId = null,
    long? CartaoId = null);
