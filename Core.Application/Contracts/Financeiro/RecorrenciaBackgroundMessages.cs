namespace Core.Application.Contracts.Financeiro;

public sealed record RateioAmigoBackgroundMessage(string Nome, decimal? Valor);
public sealed record RateioAreaBackgroundMessage(long AreaId, long SubAreaId, decimal? Valor);

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
    string? AnexoDocumento,
    IReadOnlyCollection<string> TiposRateio,
    IReadOnlyCollection<RateioAmigoBackgroundMessage> AmigosRateio,
    IReadOnlyCollection<RateioAreaBackgroundMessage> AreasRateio);

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
    string? AnexoDocumento,
    IReadOnlyCollection<RateioAmigoBackgroundMessage> AmigosRateio,
    IReadOnlyCollection<RateioAreaBackgroundMessage> AreasRateio);
