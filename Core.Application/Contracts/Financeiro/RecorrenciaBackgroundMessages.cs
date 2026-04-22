using Core.Domain.Enums.Financeiro;

namespace Core.Application.Contracts.Financeiro;

public sealed record RateioAmigoBackgroundMessage(int AmigoId, string Nome, decimal? Valor);
public sealed record RateioAreaBackgroundMessage(long AreaId, long SubAreaId, decimal? Valor);
public sealed record DocumentoBackgroundMessage(string NomeArquivo, string CaminhoArquivo, string? ContentType, long TamanhoBytes);

public sealed record DespesaRecorrenciaBackgroundMessage(
    int UsuarioId,
    long DespesaRecorrenciaOrigemId,
    string Descricao,
    string? Observacao,
    DateTime DataHoraCadastroOrigem,
    DateTime DataLancamento,
    DateOnly DataVencimento,
    TipoDespesa TipoDespesa,
    TipoPagamento TipoPagamento,
    Recorrencia Recorrencia,
    bool RecorrenciaFixa,
    int? QuantidadeRecorrencia,
    decimal ValorTotal,
    decimal Desconto,
    decimal Acrescimo,
    decimal Imposto,
    decimal Juros,
    long? ContaBancariaId,
    long? ContaDestinoId,
    long? CartaoId,
    decimal? ValorTotalRateioAmigos,
    TipoRateioAmigos? TipoRateioAmigos,
    IReadOnlyCollection<DocumentoBackgroundMessage>? Documentos,
    IReadOnlyCollection<RateioAmigoBackgroundMessage> AmigosRateio,
    IReadOnlyCollection<RateioAreaBackgroundMessage> AreasSubAreasRateio);

public sealed record ReceitaRecorrenciaBackgroundMessage(
    int UsuarioId,
    long? ReceitaRecorrenciaOrigemId,
    string Descricao,
    string? Observacao,
    DateTime DataHoraCadastroOrigem,
    DateTime DataLancamento,
    DateOnly DataVencimento,
    TipoReceita TipoReceita,
    TipoRecebimento TipoRecebimento,
    Recorrencia Recorrencia,
    bool RecorrenciaFixa,
    int? QuantidadeRecorrencia,
    decimal ValorTotal,
    decimal Desconto,
    decimal Acrescimo,
    decimal Imposto,
    decimal Juros,
    long? ContaBancariaId,
    long? ContaDestinoId,
    long? CartaoId,
    decimal? ValorTotalRateioAmigos,
    TipoRateioAmigos? TipoRateioAmigos,
    IReadOnlyCollection<DocumentoBackgroundMessage>? Documentos,
    IReadOnlyCollection<RateioAmigoBackgroundMessage> AmigosRateio,
    IReadOnlyCollection<RateioAreaBackgroundMessage> AreasSubAreasRateio);
