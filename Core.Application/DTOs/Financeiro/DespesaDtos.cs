using Core.Domain.Enums;

namespace Core.Application.DTOs.Financeiro;

public sealed record ListarDespesasRequest(string? Id, string? Descricao, string? Competencia, DateOnly? DataInicio, DateOnly? DataFim, bool VerificarUltimaRecorrencia);
public sealed record DespesaLogDto(long Id, DateOnly Data, AcaoLogs Acao, string Descricao);
public sealed record DespesaAreaRateioDto(long AreaId, string AreaNome, long SubAreaId, string SubAreaNome, decimal? Valor);
public sealed record AmigoRateioDto(int? AmigoId, string Nome, decimal? Valor);
public sealed record AmigoRateioRequest(int AmigoId, decimal? Valor);
public sealed record DespesaListaDto(long Id, string Descricao, DateOnly DataLancamento, DateOnly DataVencimento, DateOnly? DataEfetivacao, TipoDespesa TipoDespesa, TipoPagamento TipoPagamento, decimal ValorTotal, decimal ValorLiquido, decimal? ValorEfetivacao, string Status, MeioFinanceiroVinculoDto? Vinculo);
public sealed record DespesaDto(long Id, string Descricao, string? Observacao, DateOnly DataLancamento, DateOnly DataVencimento, DateOnly? DataEfetivacao, TipoDespesa TipoDespesa, TipoPagamento TipoPagamento, Recorrencia Recorrencia, int? QuantidadeRecorrencia, bool RecorrenciaFixa, decimal ValorTotal, decimal ValorLiquido, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, decimal? ValorEfetivacao, string Status, IReadOnlyCollection<AmigoRateioDto> AmigosRateio, IReadOnlyCollection<DespesaAreaRateioDto> AreasSubAreasRateio, IReadOnlyCollection<DocumentoDto> Documentos, MeioFinanceiroVinculoDto? Vinculo, IReadOnlyCollection<DespesaLogDto> Logs);

public sealed record DespesaAreaRateioRequest(long AreaId, long SubAreaId, decimal? Valor);
public sealed record CriarDespesaRequest(string Descricao, string? Observacao, DateOnly DataLancamento, DateOnly DataVencimento, TipoDespesa TipoDespesa, TipoPagamento TipoPagamento, Recorrencia Recorrencia, decimal ValorTotal, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, IReadOnlyCollection<DocumentoRequest>? Documentos = null, IReadOnlyCollection<AmigoRateioRequest>? AmigosRateio = null, IReadOnlyCollection<DespesaAreaRateioRequest>? AreasSubAreasRateio = null, int? QuantidadeRecorrencia = null, int? QuantidadeParcelas = null, bool RecorrenciaFixa = false, decimal? ValorLiquido = null, MeioFinanceiroVinculoRequest? Vinculo = null, decimal? ValorTotalRateioAmigos = null);
public sealed record AtualizarDespesaRequest(string Descricao, string? Observacao, DateOnly DataLancamento, DateOnly DataVencimento, TipoDespesa TipoDespesa, TipoPagamento TipoPagamento, Recorrencia Recorrencia, decimal ValorTotal, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, IReadOnlyCollection<DocumentoRequest>? Documentos = null, IReadOnlyCollection<AmigoRateioRequest>? AmigosRateio = null, IReadOnlyCollection<DespesaAreaRateioRequest>? AreasSubAreasRateio = null, int? QuantidadeRecorrencia = null, int? QuantidadeParcelas = null, bool RecorrenciaFixa = false, decimal? ValorLiquido = null, MeioFinanceiroVinculoRequest? Vinculo = null, decimal? ValorTotalRateioAmigos = null);
public sealed record EfetivarDespesaRequest(
    DateOnly DataEfetivacao,
    TipoPagamento TipoPagamento,
    decimal ValorTotal,
    decimal Desconto,
    decimal Acrescimo,
    decimal Imposto,
    decimal Juros,
    IReadOnlyCollection<DocumentoRequest>? Documentos = null,
    long? ContaBancariaId = null,
    long? CartaoId = null,
    MeioFinanceiroVinculoRequest? Vinculo = null);
