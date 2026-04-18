namespace Core.Domain.Common;

public static class DataHoraBrasil
{
    private static readonly TimeZoneInfo FusoHorarioBrasil = ResolverFusoHorarioBrasil();

    public static DateTime Agora() => Converter(DateTime.UtcNow);

    public static DateOnly Hoje() => DateOnly.FromDateTime(Agora());

    public static DateTime Converter(DateTime dataHora)
    {
        if (dataHora == default)
            return dataHora;

        return dataHora.Kind switch
        {
            DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(dataHora, FusoHorarioBrasil),
            DateTimeKind.Local => TimeZoneInfo.ConvertTime(dataHora, TimeZoneInfo.Local, FusoHorarioBrasil),
            _ => dataHora
        };
    }

    private static TimeZoneInfo ResolverFusoHorarioBrasil()
    {
        var ids = new[]
        {
            "America/Sao_Paulo",
            "E. South America Standard Time"
        };

        foreach (var id in ids)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        throw new InvalidOperationException("Nao foi possivel resolver o fuso horario do Brasil.");
    }
}
