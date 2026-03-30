using System.Globalization;
using Core.Domain.Exceptions;

namespace Core.Application.Services.Financeiro;

internal static class CompetenciaPeriodoHelper
{
    public static (DateOnly? DataInicio, DateOnly? DataFim) Resolver(string? competencia, DateOnly? dataInicio, DateOnly? dataFim)
    {
        if (!string.IsNullOrWhiteSpace(competencia))
        {
            if (!DateTime.TryParseExact(
                    competencia.Trim(),
                    "MM/yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var competenciaData))
            {
                throw new DomainException("dados_invalidos");
            }

            var inicioCompetencia = new DateOnly(competenciaData.Year, competenciaData.Month, 1);
            var fimCompetencia = new DateOnly(
                competenciaData.Year,
                competenciaData.Month,
                DateTime.DaysInMonth(competenciaData.Year, competenciaData.Month));

            return (inicioCompetencia, fimCompetencia);
        }

        if (dataInicio.HasValue && dataFim.HasValue && dataFim.Value < dataInicio.Value)
            throw new DomainException("periodo_invalido");

        if (!dataInicio.HasValue && !dataFim.HasValue)
        {
            var hoje = DateOnly.FromDateTime(DateTime.Today);
            var inicioMesAtual = new DateOnly(hoje.Year, hoje.Month, 1);
            var fimMesAtual = new DateOnly(hoje.Year, hoje.Month, DateTime.DaysInMonth(hoje.Year, hoje.Month));
            return (inicioMesAtual, fimMesAtual);
        }

        return (dataInicio, dataFim);
    }
}
