namespace DatabaseAnalyzer.AnalyzerHelpers.Extensions;

public static class StringExtensions
{
    public static string? NullIfEmptyOrWhiteSpace(this string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value;
}
