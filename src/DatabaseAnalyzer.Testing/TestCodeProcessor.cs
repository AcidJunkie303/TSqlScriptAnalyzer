using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing;

public static partial class TestCodeProcessor
{
    [GeneratedRegex(@"\{\{(?<id>[^¦]+)¦(?<parts>[^\|]+)\|(?<code>.*?)\}\}", RegexOptions.Compiled, 1000)]
    private static partial Regex MarkupRegex();

    public static TestCode ParseTestCode(string code)
    {
        var issues = new List<IIssue>();

        var markupFreeCode = MarkupRegex().Replace(code, match =>
        {
            var id = match.Groups["id"].Value;
            var parts = match.Groups["parts"].Value.Split('¦');

            continue here
            // remove markup block and get final locations (start and end) and put that into the issue

            return id + parts[0]; // just that it compiles. to remove
        });

        return new TestCode(markupFreeCode, issues);
    }
}
