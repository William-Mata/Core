using System.Globalization;
using Core.Domain.Exceptions;

namespace Core.Application.Services.Financeiro;

internal static class CompetenciaPeriodoHelper
{
    public static (DateOnly? DataInicio, DateOnly? DataFim) Resolver(string? competencia, DateOnly? dataInicio, DateOnly? dataFim)
    {
        if (!string.IsNullOrWhiteSpace(competencia))
        {
            if (!TryParseCompetencia(competencia, out var competenciaData))
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

    private static readonly string[] FormatosCompetencia =
      [
          "MM/yyyy", "M/yyyy",
        "MM-yyyy", "M-yyyy",
        "MM.yyyy", "M.yyyy",
        "yyyy/MM", "yyyy/M",
        "yyyy-MM", "yyyy-M",
        "yyyy.MM", "yyyy.M"
      ];

    private static bool TryParseCompetencia(string? competencia, out DateTime competenciaData)
    {
        competenciaData = default;

        if (string.IsNullOrWhiteSpace(competencia))
            return false;

        var valor = competencia.Trim();

        if (DateTime.TryParseExact(
                valor,
                FormatosCompetencia,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var data))
        {
            competenciaData = new DateTime(data.Year, data.Month, 1);
            return true;
        }

        var apenasDigitos = new string(valor.Where(char.IsDigit).ToArray());

        if (apenasDigitos.Length == 6)
        {
            // MMYYYY
            if (int.TryParse(apenasDigitos[..2], out var mesA) &&
                int.TryParse(apenasDigitos[2..], out var anoA) &&
                mesA is >= 1 and <= 12 &&
                anoA is >= 1900 and <= 2100)
            {
                competenciaData = new DateTime(anoA, mesA, 1);
                return true;
            }

            // YYYYMM
            if (int.TryParse(apenasDigitos[..4], out var anoB) &&
                int.TryParse(apenasDigitos[4..], out var mesB) &&
                mesB is >= 1 and <= 12 &&
                anoB is >= 1900 and <= 2100)
            {
                competenciaData = new DateTime(anoB, mesB, 1);
                return true;
            }
        }

        return false;
    }
}
