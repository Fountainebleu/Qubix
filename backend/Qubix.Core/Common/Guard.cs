namespace Qubix.Core.Common;

internal static class Guard
{
    public static Guid NotEmpty(Guid value, string parameterName)
    {
        return value == Guid.Empty
            ? throw new ArgumentException("Value cannot be an empty identifier.", parameterName)
            : value;
    }

    public static string Required(
        string? value,
        string parameterName,
        int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrEmpty(normalized))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value cannot be longer than {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    public static string? Optional(
        string? value,
        string parameterName,
        int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value cannot be longer than {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    public static int InRange(
        int value,
        int minimum,
        int maximum,
        string parameterName)
    {
        return value < minimum || value > maximum
            ? throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Value must be between {minimum} and {maximum}.")
            : value;
    }

    public static int NonNegative(int value, string parameterName)
    {
        return value < 0
            ? throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Value cannot be negative.")
            : value;
    }

    public static DateTimeOffset Utc(DateTimeOffset value, string parameterName)
    {
        return value.Offset != TimeSpan.Zero
            ? throw new ArgumentException("Timestamp must use the UTC offset.", parameterName)
            : value;
    }

    public static TEnum DefinedEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        return Enum.IsDefined(value)
            ? value
            : throw new ArgumentOutOfRangeException(parameterName, value, "Unknown enum value.");
    }
}
