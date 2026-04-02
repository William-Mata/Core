using System.Globalization;
using Core.Domain.Exceptions;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

internal static class CompetenciaFiltroHelper
{
    private static readonly string[] FormatosCompetencia =
    [
        "MM/yyyy", "M/yyyy",
        "MM-yyyy", "M-yyyy",
        "MM.yyyy", "M.yyyy",
        "yyyy/MM", "yyyy/M",
        "yyyy-MM", "yyyy-M",
        "yyyy.MM", "yyyy.M"
    ];

    public static (int Ano, int Mes)? ResolverMesAno(string? competencia)
    {
        if (string.IsNullOrWhiteSpace(competencia))
            return null;

        if (!TryParseCompetencia(competencia, out var data))
            throw new DomainException("dados_invalidos");

        return (data.Year, data.Month);
    }

    private static bool TryParseCompetencia(string competencia, out DateTime competenciaData)
    {
        competenciaData = default;
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
        if (apenasDigitos.Length != 6)
            return false;

        if (int.TryParse(apenasDigitos[..2], out var mesA) &&
            int.TryParse(apenasDigitos[2..], out var anoA) &&
            mesA is >= 1 and <= 12 &&
            anoA is >= 1900 and <= 2100)
        {
            competenciaData = new DateTime(anoA, mesA, 1);
            return true;
        }

        if (int.TryParse(apenasDigitos[..4], out var anoB) &&
            int.TryParse(apenasDigitos[4..], out var mesB) &&
            mesB is >= 1 and <= 12 &&
            anoB is >= 1900 and <= 2100)
        {
            competenciaData = new DateTime(anoB, mesB, 1);
            return true;
        }

        return false;
    }
}
