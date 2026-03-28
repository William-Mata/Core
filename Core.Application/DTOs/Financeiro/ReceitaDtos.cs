using Core.Domain.Enums;

namespace Core.Application.DTOs.Financeiro;

public sealed record ReceitaLogDto(long Id, DateOnly Data, AcaoLogs Acao, string Descricao);
public sealed record ReceitaAreaRateioDto(long AreaId, string AreaNome, long SubAreaId, string SubAreaNome, decimal? Valor);
public sealed record ReceitaDto(long Id, string Descricao, string? Observacao, DateOnly DataLancamento, DateOnly DataVencimento, DateOnly? DataEfetivacao, string TipoReceita, string TipoRecebimento, Recorrencia Recorrencia, int? QuantidadeRecorrencia, decimal ValorTotal, decimal ValorLiquido, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, decimal? ValorEfetivacao, string Status, IReadOnlyCollection<string> AmigosRateio, IReadOnlyDictionary<string, decimal> RateioAmigosValores, IReadOnlyCollection<ReceitaAreaRateioDto> AreasRateio, string? ContaBancaria, string? AnexoDocumento, IReadOnlyCollection<ReceitaLogDto> Logs);

public sealed record ReceitaAreaRateioRequest(long AreaId, long SubAreaId, decimal? Valor);
public sealed record CriarReceitaRequest(string Descricao, string? Observacao, DateOnly DataLancamento, DateOnly DataVencimento, string TipoReceita, string TipoRecebimento, Recorrencia Recorrencia, decimal ValorTotal, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, IReadOnlyCollection<string> AmigosRateio, IReadOnlyDictionary<string, decimal> RateioAmigosValores, IReadOnlyCollection<ReceitaAreaRateioRequest> AreasRateio, string? ContaBancaria, string? AnexoDocumento, int? QuantidadeRecorrencia = null);
public sealed record AtualizarReceitaRequest(string Descricao, string? Observacao, DateOnly DataLancamento, DateOnly DataVencimento, string TipoReceita, string TipoRecebimento, Recorrencia Recorrencia, decimal ValorTotal, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, IReadOnlyCollection<string> AmigosRateio, IReadOnlyDictionary<string, decimal> RateioAmigosValores, IReadOnlyCollection<ReceitaAreaRateioRequest> AreasRateio, string? ContaBancaria, string? AnexoDocumento, int? QuantidadeRecorrencia = null);
public sealed record EfetivarReceitaRequest(DateOnly DataEfetivacao, string TipoRecebimento, string? ContaBancaria, decimal ValorTotal, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, string? AnexoDocumento);
