using System.Text.RegularExpressions;

namespace DatabaseAnalyzer.Contracts;

public static partial class InsertionStringHelpers
{
    [GeneratedRegex(@"\{\s*\d+\s*\}", RegexOptions.None, 100)]
    private static partial Regex InsertionStringsFinder();

    public static int CountInsertionStringPlaceholders(string messageTemplate) => InsertionStringsFinder().Matches(messageTemplate).Count;

    public static string FormatMessage(string messageTemplate, IReadOnlyList<string> insertionStrings)
    {
        if (insertionStrings.Count == 0)
        {
            return messageTemplate;
        }

        var message = messageTemplate;
        for (var i = 0; i < insertionStrings.Count; i++)
        {
            var keyToFind = $"{{{i}}}";
            message = message.Replace(keyToFind, insertionStrings[i], StringComparison.Ordinal);
        }

        return message;
    }
}
