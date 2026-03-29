using Core.Domain.Enums;

namespace Core.Application.DTOs.Financeiro;

public sealed record DespesaLogDto(long Id, DateOnly Data, AcaoLogs Acao, string Descricao);
public sealed record DespesaAreaRateioDto(long AreaId, string AreaNome, long SubAreaId, string SubAreaNome, decimal? Valor);
public sealed record AmigoRateioDto(string Nome, decimal? Valor);
public sealed record AmigoRateioRequest(string Nome, decimal? Valor);
public sealed record DespesaDto(long Id, string Descricao, string? Observacao, DateOnly DataLancamento, DateOnly DataVencimento, DateOnly? DataEfetivacao, string TipoDespesa, string TipoPagamento, Recorrencia Recorrencia, int? QuantidadeRecorrencia, bool RecorrenciaFixa, decimal ValorTotal, decimal ValorLiquido, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, decimal? ValorEfetivacao, string Status, IReadOnlyCollection<AmigoRateioDto> AmigosRateio, IReadOnlyCollection<DespesaAreaRateioDto> AreasSubAreasRateio, IReadOnlyCollection<DocumentoDto> Documentos, IReadOnlyCollection<DespesaLogDto> Logs);

public sealed record DespesaAreaRateioRequest(long AreaId, long SubAreaId, decimal? Valor);
public sealed record CriarDespesaRequest(string Descricao, string? Observacao, DateOnly DataLancamento, DateOnly DataVencimento, string TipoDespesa, string TipoPagamento, Recorrencia Recorrencia, decimal ValorTotal, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, IReadOnlyCollection<DocumentoRequest>? Documentos = null, IReadOnlyCollection<AmigoRateioRequest>? AmigosRateio = null, IReadOnlyCollection<DespesaAreaRateioRequest>? AreasSubAreasRateio = null, int? QuantidadeRecorrencia = null, int? QuantidadeParcelas = null, bool RecorrenciaFixa = false, decimal? ValorLiquido = null);
public sealed record AtualizarDespesaRequest(string Descricao, string? Observacao, DateOnly DataLancamento, DateOnly DataVencimento, string TipoDespesa, string TipoPagamento, Recorrencia Recorrencia, decimal ValorTotal, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, IReadOnlyCollection<DocumentoRequest>? Documentos = null, IReadOnlyCollection<AmigoRateioRequest>? AmigosRateio = null, IReadOnlyCollection<DespesaAreaRateioRequest>? AreasSubAreasRateio = null, int? QuantidadeRecorrencia = null, int? QuantidadeParcelas = null, bool RecorrenciaFixa = false, decimal? ValorLiquido = null);
public sealed record EfetivarDespesaRequest(
    DateOnly DataEfetivacao,
    string TipoPagamento,
    decimal ValorTotal,
    decimal Desconto,
    decimal Acrescimo,
    decimal Imposto,
    decimal Juros,
    IReadOnlyCollection<DocumentoRequest>? Documentos = null,
    long? ContaBancariaId = null,
    long? CartaoId = null);
