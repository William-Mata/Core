using System.Text.Json;

namespace Core.Application.DTOs;

public sealed record ListarReembolsosRequest(string? Id, string? Descricao, DateOnly? DataInicio, DateOnly? DataFim);

public sealed record ReembolsoDto(
    long Id,
    string Descricao,
    string Solicitante,
    DateOnly DataSolicitacao,
    IReadOnlyCollection<long> DespesasVinculadas,
    decimal ValorTotal,
    string Status)
{
    public string SolicitanteName => Solicitante;
}

public sealed record SalvarReembolsoRequest(
    string Descricao,
    string Solicitante,
    DateOnly DataSolicitacao,
    IReadOnlyCollection<JsonElement> DespesasVinculadas,
    decimal? ValorTotal,
    string? Status);
