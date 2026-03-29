using System.Text.Json;

namespace Core.Application.DTOs.Financeiro;

public sealed record ListarReembolsosRequest(string? Id, string? Descricao, DateOnly? DataInicio, DateOnly? DataFim);

public sealed record ReembolsoDto(
    long Id,
    string Descricao,
    string Solicitante,
    DateOnly DataLancamento,
    DateOnly? DataEfetivacao,
    IReadOnlyCollection<long> DespesasVinculadas,
    decimal ValorTotal,
    string Status);

public sealed record SalvarReembolsoRequest(
    string Descricao,
    string Solicitante,
    DateOnly DataLancamento,
    DateOnly? DataEfetivacao,
    IReadOnlyCollection<JsonElement> DespesasVinculadas,
    decimal? ValorTotal,
    string? Status,
    long? ContaBancariaId = null,
    long? CartaoId = null);

public sealed record EfetivarReembolsoRequest(
    DateOnly DataEfetivacao,
    long? ContaBancariaId = null,
    long? CartaoId = null);
