using System;

namespace Core.Application.Extensions;

public static class EnumExtensions
{
    public static string ToCamelCase(this Enum value)
    {
        var text = value.ToString();
        if (string.IsNullOrEmpty(text))
            return text;

        return char.ToLowerInvariant(text[0]) + text[1..];
    }

    public static string? ToCamelCase<TEnum>(this TEnum? value)
        where TEnum : struct, Enum
    {
        if (!value.HasValue)
            return null;

        return ToCamelCase(value.Value);
    }
}
