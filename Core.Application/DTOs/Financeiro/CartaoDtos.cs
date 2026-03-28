using Core.Domain.Enums;

namespace Core.Application.DTOs;

public sealed record CartaoLancamentoDto(long Id, DateOnly Data, string Descricao, decimal Valor);
public sealed record CartaoLogDto(long Id, DateOnly Data, AcaoLogs Acao, string Descricao);
public sealed record CartaoDto(long Id, string Descricao, string Bandeira, TipoCartao Tipo, decimal? Limite, decimal SaldoDisponivel, DateOnly? DiaVencimento, DateOnly? DataVencimentoCartao, string Status, IReadOnlyCollection<CartaoLancamentoDto> Lancamentos, IReadOnlyCollection<CartaoLogDto> Logs);

public sealed record CriarCartaoRequest(string Descricao, string Bandeira, TipoCartao Tipo, decimal? Limite, decimal SaldoDisponivel, DateOnly? DiaVencimento, DateOnly? DataVencimentoCartao);
public sealed record AtualizarCartaoRequest(string Descricao, string Bandeira, TipoCartao Tipo, decimal? Limite, DateOnly? DiaVencimento, DateOnly? DataVencimentoCartao);
public sealed record AlternarStatusCartaoRequest(int QuantidadePendencias = 0);
