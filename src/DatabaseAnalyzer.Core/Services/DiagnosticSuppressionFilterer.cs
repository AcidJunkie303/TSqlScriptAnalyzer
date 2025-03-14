using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Core.Models;

namespace DatabaseAnalyzer.Core.Services;

internal static class DiagnosticSuppressionFilterer
{
    public static (IReadOnlyList<IIssue> Issues, IReadOnlyList<SuppressedIssue> SuppressedIssues)
        Filter(IScriptModel script, IEnumerable<IIssue> issues)
    {
        var suppressionMap = new SuppressionMap(script.DiagnosticSuppressions);
        var unsuppressedIssues = new List<IIssue>();
        var suppressedIssues = new List<SuppressedIssue>();

        foreach (var issue in issues)
        {
            var activeSuppressions = suppressionMap
                .GetActiveSuppressionsAtLocation(issue.CodeRegion.Begin.Line, issue.CodeRegion.Begin.Column);

            var suppression = activeSuppressions.LastOrDefault(a => a.DiagnosticId.EqualsOrdinalIgnoreCase(issue.DiagnosticDefinition.DiagnosticId));
            if (suppression is null)
            {
                unsuppressedIssues.Add(issue);
            }
            else
            {
                suppressedIssues.Add(new SuppressedIssue(issue, suppression.Reason));
            }
        }

        return (unsuppressedIssues, suppressedIssues);
    }
}
