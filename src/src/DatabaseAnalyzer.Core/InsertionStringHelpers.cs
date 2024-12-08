using System.Text.RegularExpressions;

namespace DatabaseAnalyzer.Core;

internal static partial class InsertionStringHelpers
{
    [GeneratedRegex(@"\{\s*\d+\s*\}", RegexOptions.None, 100)]
    private static partial Regex InsertionStringsFinder();

    public static int CountInsertionStrings(string messageTemplate) => InsertionStringsFinder().Matches(messageTemplate).Count;
}
