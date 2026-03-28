using Core.Domain.Enums;

namespace Core.Application.DTOs;

public sealed record ContaBancariaExtratoDto(long Id, DateOnly Data, string Descricao, string Tipo, decimal Valor);
public sealed record ContaBancariaLogDto(long Id, DateOnly Data, AcaoLogs Acao, string Descricao);
public sealed record ContaBancariaDto(long Id, string Descricao, string Banco, string Agencia, string Numero, decimal SaldoInicial, decimal SaldoAtual, DateOnly DataAbertura, string Status, IReadOnlyCollection<ContaBancariaExtratoDto> Extrato, IReadOnlyCollection<ContaBancariaLogDto> Logs);

public sealed record CriarContaBancariaRequest(string Descricao, string Banco, string Agencia, string Numero, decimal SaldoInicial, DateOnly DataAbertura);
public sealed record AtualizarContaBancariaRequest(string Descricao, string Banco, string Agencia, string Numero, DateOnly DataAbertura);
public sealed record AlternarStatusContaBancariaRequest(int QuantidadePendencias = 0);
