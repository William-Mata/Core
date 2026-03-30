namespace Core.Application.Contracts.Financeiro;

public sealed record RateioAmigoBackgroundMessage(int AmigoId, string Nome, decimal? Valor);
public sealed record RateioAreaBackgroundMessage(long AreaId, long SubAreaId, decimal? Valor);
public sealed record DocumentoBackgroundMessage(string NomeArquivo, string CaminhoArquivo, string? ContentType, long TamanhoBytes);

public sealed record DespesaRecorrenciaBackgroundMessage(
    int UsuarioId,
    string Descricao,
    string? Observacao,
    DateOnly DataLancamento,
    DateOnly DataVencimento,
    string TipoDespesa,
    string TipoPagamento,
    Core.Domain.Enums.Recorrencia Recorrencia,
    bool RecorrenciaFixa,
    int? QuantidadeRecorrencia,
    decimal ValorTotal,
    decimal Desconto,
    decimal Acrescimo,
    decimal Imposto,
    decimal Juros,
    IReadOnlyCollection<DocumentoBackgroundMessage>? Documentos,
    IReadOnlyCollection<RateioAmigoBackgroundMessage> AmigosRateio,
    IReadOnlyCollection<RateioAreaBackgroundMessage> AreasSubAreasRateio);

public sealed record ReceitaRecorrenciaBackgroundMessage(
    int UsuarioId,
    string Descricao,
    string? Observacao,
    DateOnly DataLancamento,
    DateOnly DataVencimento,
    string TipoReceita,
    string TipoRecebimento,
    Core.Domain.Enums.Recorrencia Recorrencia,
    bool RecorrenciaFixa,
    int? QuantidadeRecorrencia,
    decimal ValorTotal,
    decimal Desconto,
    decimal Acrescimo,
    decimal Imposto,
    decimal Juros,
    string? ContaBancaria,
    IReadOnlyCollection<DocumentoBackgroundMessage>? Documentos,
    IReadOnlyCollection<RateioAmigoBackgroundMessage> AmigosRateio,
    IReadOnlyCollection<RateioAreaBackgroundMessage> AreasSubAreasRateio);
