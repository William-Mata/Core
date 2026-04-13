using Core.Domain.Enums;

namespace Core.Application.DTOs.Financeiro;

public sealed record ListarDespesasRequest(
    string? Id,
    string? Descricao,
    string? Competencia,
    DateOnly? DataInicio,
    DateOnly? DataFim,
    bool VerificarUltimaRecorrencia,
    bool DesconsiderarVinculadosCartaoCredito = false,
    bool DesconsiderarCancelados = false);
public sealed record DespesaLogDto(long Id, DateOnly Data, AcaoLogs Acao, string Descricao);
public sealed record DespesaAreaRateioDto(long AreaId, string AreaNome, long SubAreaId, string SubAreaNome, decimal? Valor);
public sealed record AmigoRateioDto(int? AmigoId, string Nome, decimal? Valor);
public sealed record AmigoRateioRequest(int AmigoId, decimal? Valor);
public sealed record DespesaListaDto(long Id, string Descricao, string Competencia, DateOnly DataLancamento, DateOnly DataVencimento, DateOnly? DataEfetivacao, TipoDespesa TipoDespesa, TipoPagamento TipoPagamento, decimal ValorTotal, decimal ValorLiquido, decimal? ValorEfetivacao, string Status, long? ContaBancariaId, long? ContaDestinoId, long? CartaoId);
public sealed record DespesaDto(long Id, string Descricao, string? Observacao, string Competencia, DateOnly DataLancamento, DateOnly DataVencimento, DateOnly? DataEfetivacao, TipoDespesa TipoDespesa, TipoPagamento TipoPagamento, Recorrencia Recorrencia, int? QuantidadeRecorrencia, bool RecorrenciaFixa, decimal ValorTotal, decimal? ValorTotalRateioAmigos, decimal ValorLiquido, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, decimal? ValorEfetivacao, string Status, TipoRateioAmigos? TipoRateioAmigos, IReadOnlyCollection<AmigoRateioDto> AmigosRateio, IReadOnlyCollection<DespesaAreaRateioDto> AreasSubAreasRateio, long? ContaBancariaId, long? ContaDestinoId, long? CartaoId, IReadOnlyCollection<DocumentoDto> Documentos, IReadOnlyCollection<DespesaLogDto> Logs);

public sealed record DespesaAreaRateioRequest(long AreaId, long SubAreaId, decimal? Valor);
public sealed record CriarDespesaRequest(string Descricao, string? Observacao, string? Competencia, DateOnly DataLancamento, DateOnly DataVencimento, TipoDespesa TipoDespesa, TipoPagamento TipoPagamento, Recorrencia Recorrencia, decimal ValorTotal, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, IReadOnlyCollection<DocumentoRequest>? Documentos = null, IReadOnlyCollection<AmigoRateioRequest>? AmigosRateio = null, IReadOnlyCollection<DespesaAreaRateioRequest>? AreasSubAreasRateio = null, int? QuantidadeRecorrencia = null, int? QuantidadeParcelas = null, bool RecorrenciaFixa = false, decimal? ValorLiquido = null, long? ContaBancariaId = null, long? ContaDestinoId = null, long? CartaoId = null, decimal? ValorTotalRateioAmigos = null, TipoRateioAmigos? TipoRateioAmigos = null);
public sealed record AtualizarDespesaRequest(string Descricao, string? Observacao, string? Competencia, DateOnly DataLancamento, DateOnly DataVencimento, TipoDespesa TipoDespesa, TipoPagamento TipoPagamento, Recorrencia Recorrencia, decimal ValorTotal, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, IReadOnlyCollection<DocumentoRequest>? Documentos = null, IReadOnlyCollection<AmigoRateioRequest>? AmigosRateio = null, IReadOnlyCollection<DespesaAreaRateioRequest>? AreasSubAreasRateio = null, int? QuantidadeRecorrencia = null, int? QuantidadeParcelas = null, bool RecorrenciaFixa = false, decimal? ValorLiquido = null, long? ContaBancariaId = null, long? ContaDestinoId = null, long? CartaoId = null, decimal? ValorTotalRateioAmigos = null, TipoRateioAmigos? TipoRateioAmigos = null);
public sealed record EfetivarDespesaRequest(DateOnly DataEfetivacao, TipoPagamento TipoPagamento, decimal ValorTotal, decimal Desconto, decimal Acrescimo, decimal Imposto, decimal Juros, IReadOnlyCollection<DocumentoRequest>? Documentos = null, long? ContaBancariaId = null, long? CartaoId = null, string? ObservacaoHistorico = null, long? ContaDestinoId = null);
public sealed record EstornarDespesaRequest(DateOnly DataEstorno, string? ObservacaoHistorico = null, bool OcultarDoHistorico = true, long? ContaDestinoId = null);
