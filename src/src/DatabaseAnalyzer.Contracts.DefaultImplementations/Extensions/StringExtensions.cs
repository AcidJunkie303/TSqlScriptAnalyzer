using System.Text.RegularExpressions;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class StringExtensions
{
    public static Regex ToRegexWithSimpleWildcards(this string value, bool caseSensitive = false, bool compileRegex = false)
    {
        var pattern = Regex.Escape(value)
            .Replace("\\*", ".*", StringComparison.Ordinal) // Convert '*' to '.*'
            .Replace("\\?", ".", StringComparison.Ordinal);

        var options = RegexOptions.None;
        if (!caseSensitive)
        {
            options |= RegexOptions.IgnoreCase;
        }

        if (compileRegex)
        {
            options |= RegexOptions.Compiled;
        }

        return new Regex(pattern, options, TimeSpan.FromSeconds(1));
    }
}
