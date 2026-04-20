namespace Core.Application.DTOs.Financeiro;

public sealed record ListarFaturasCartaoRequest(long? CartaoId, string Competencia);

public sealed record ListarFaturasCartaoDetalheRequest(string Competencia, string? TipoTransacao);

public sealed record EfetivarFaturaCartaoRequest(
    DateTime DataEfetivacao,
    long ContaBancariaId,
    decimal ValorTotal,
    decimal ValorEfetivacao,
    string? ObservacaoHistorico = null);

public sealed record EstornarFaturaCartaoRequest(
    DateTime DataEstorno,
    string? ObservacaoHistorico = null,
    bool OcultarDoHistorico = true);

public sealed record FaturaCartaoListaDto(
    long Id,
    long CartaoId,
    string Competencia,
    DateOnly? DataVencimento,
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
    DateTime DataLancamento,
    DateTime? DataEfetivacao,
    decimal Valor,
    string Status);

public sealed record FaturaCartaoDetalheDto(
    long Id,
    long CartaoId,
    string Competencia,
    DateOnly? DataVencimento,
    decimal ValorTotal,
    decimal ValorTotalTransacoes,
    string Status,
    DateOnly? DataFechamento,
    DateOnly? DataEfetivacao,
    DateOnly? DataEstorno,
    IReadOnlyCollection<FaturaCartaoLancamentoDto> Lancamentos);
