namespace Core.Application.DTOs.Financeiro;

public sealed record ListarFaturasCartaoRequest(long? CartaoId, string Competencia);

public sealed record ListarFaturasCartaoDetalheRequest(string Competencia, string? TipoTransacao);

public sealed record FaturaCartaoListaDto(
    long Id,
    long CartaoId,
    string Competencia,
    decimal ValorTotal,
    string Status,
    DateOnly? DataFechamento,
    DateOnly? DataEfetivacao,
    DateOnly? DataEstorno);

public sealed record FaturaCartaoLancamentoDto(
    string TipoTransacao,
    long TransacaoId,
    string Descricao,
    string Competencia,
    DateOnly DataLancamento,
    DateOnly? DataEfetivacao,
    decimal Valor,
    string Status);

public sealed record FaturaCartaoDetalheDto(
    long Id,
    long CartaoId,
    string Competencia,
    decimal ValorTotal,
    decimal ValorTotalTransacoes,
    string Status,
    DateOnly? DataFechamento,
    DateOnly? DataEfetivacao,
    DateOnly? DataEstorno,
    IReadOnlyCollection<FaturaCartaoLancamentoDto> Lancamentos);
