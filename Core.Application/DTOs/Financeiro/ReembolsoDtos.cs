using System.Text.Json;

namespace Core.Application.DTOs.Financeiro;

public sealed record ListarReembolsosRequest(
    string? Id,
    string? Descricao,
    string? Competencia,
    DateOnly? DataInicio,
    DateOnly? DataFim,
    bool DesconsiderarVinculadosCartaoCredito = false,
    bool DesconsiderarCancelados = false);
public sealed record ReembolsoListaDto(
    long Id,
    string Descricao,
    string Solicitante,
    string Competencia,
    DateTime DataLancamento,
    DateOnly? DataVencimento,
    DateTime? DataEfetivacao,
    decimal ValorTotal,
    string Status);

public sealed record ReembolsoDto(
    long Id,
    string Descricao,
    string Solicitante,
    string Competencia,
    DateTime DataLancamento,
    DateOnly? DataVencimento,
    DateTime? DataEfetivacao,
    IReadOnlyCollection<long> DespesasVinculadas,
    IReadOnlyCollection<DocumentoDto> Documentos,
    decimal ValorTotal,
    string Status);

public sealed record SalvarReembolsoRequest(
    string Descricao,
    string Solicitante,
    string? Competencia,
    DateTime DataLancamento,
    DateTime? DataEfetivacao,
    IReadOnlyCollection<JsonElement> DespesasVinculadas,
    decimal? ValorTotal,
    string? Status,
    IReadOnlyCollection<DocumentoRequest>? Documentos = null,
    long? ContaBancariaId = null,
    long? CartaoId = null);

public sealed record EfetivarReembolsoRequest(
    DateTime DataEfetivacao,
    IReadOnlyCollection<DocumentoRequest>? Documentos = null,
    long? ContaBancariaId = null,
    long? CartaoId = null,
    string? ObservacaoHistorico = null);

public sealed record EstornarReembolsoRequest(
    DateOnly DataEstorno,
    string? ObservacaoHistorico = null,
    bool OcultarDoHistorico = true);
