using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Core.Services;

internal interface ISuppressionFilterer
{
    (IReadOnlyList<IIssue> Issues, IReadOnlyList<IIssue> SuppressedIssues)
        Filter(IEnumerable<IIssue> issues, IEnumerable<ScriptModel> scripts);
}

internal sealed class SuppressionFilterer : ISuppressionFilterer
{
    public (IReadOnlyList<IIssue> Issues, IReadOnlyList<IIssue> SuppressedIssues)
        Filter(IEnumerable<IIssue> issues, IEnumerable<ScriptModel> scripts)
    {
        var issuesByFullScriptFileName = issues
            .GroupBy(a => a.FullScriptFilePath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(a => a.Key, a => a.ToList(), StringComparer.OrdinalIgnoreCase);

        var scriptWithIssues = scripts
            .Select(script =>
            {
                if (!issuesByFullScriptFileName.TryGetValue(script.FullScriptFilePath, out var scriptIssues))
                {
                }
            })


        var unsuppressedIssues = new List<IIssue>();
        var suppressedIssues = new List<IIssue>();

        foreach (var issue in issues)
        {
            Console.Write($"TODO {issue}");
        }

        return (unsuppressedIssues, suppressedIssues);
    }
}
