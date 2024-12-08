using System.Diagnostics.CodeAnalysis;

namespace DatabaseAnalyzer.AnalyzerHelpers.Extensions;

public static class StringExtensions
{
    public static string? NullIfEmptyOrWhiteSpace(this string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value;

    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value)
        => string.IsNullOrWhiteSpace(value);

    public static bool EqualsOrdinal(this string? value, string? other)
        => string.Equals(value, other, StringComparison.Ordinal);

    public static bool EqualsOrdinalIgnoreCase(this string? value, string? other)
        => string.Equals(value, other, StringComparison.OrdinalIgnoreCase);
}
