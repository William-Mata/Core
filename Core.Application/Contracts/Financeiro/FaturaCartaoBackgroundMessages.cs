namespace Core.Application.Contracts.Financeiro;

public sealed record FaturaCartaoGarantiaSaneamentoBackgroundMessage(
    int UsuarioId,
    string Competencia);

