using System.Globalization;
using System.Text;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Core;

namespace DatabaseAnalyzer.App.Reporting;

internal sealed class TextReportRenderer : IReportRenderer
{
    public Task<string> RenderReportAsync(AnalysisResult analysisResult)
    {
        var buffer = new StringBuilder();

        if (analysisResult.Issues.Count == 0)
        {
            buffer.AppendLine("No issues found.");
        }
        else
        {
            buffer.AppendLine(CultureInfo.CurrentCulture, $"{analysisResult.Issues.Count} issue(s) found:");

            foreach (var issue in OrderIssues(analysisResult.Issues, static a => a))
            {
                buffer.AppendLine(CultureInfo.CurrentCulture, $"""{issue.DiagnosticDefinition.DiagnosticId} File="{issue.RelativeScriptFilePath}" Issue="{issue.Message}" Location="{issue.CodeRegion}" HelpUrl="{issue.DiagnosticDefinition.HelpUrl}" """);
            }
        }

        if (analysisResult.SuppressedIssues.Count == 0)
        {
            buffer.AppendLine("No suppressed issues found.");
        }
        else
        {
            buffer.AppendLine(CultureInfo.CurrentCulture, $"{analysisResult.SuppressedIssues.Count} suppressed issue(s) found:");
            foreach (var issue in OrderIssues(analysisResult.SuppressedIssues, static a => a.Issue))
            {
                buffer.AppendLine(CultureInfo.CurrentCulture, $"    {issue.Issue.DiagnosticDefinition.DiagnosticId} {issue.Issue.RelativeScriptFilePath} {issue.Issue.Message}.    Reason={issue.Reason}");
            }
        }

        return Task.FromResult(buffer.ToString().Trim());
    }

    private static IEnumerable<T> OrderIssues<T>(IEnumerable<T> items, Func<T, IIssue> issueSelector)
        => items
            .OrderBy(a => issueSelector(a).RelativeScriptFilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => issueSelector(a).CodeRegion);
}
