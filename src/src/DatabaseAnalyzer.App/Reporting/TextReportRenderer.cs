using System.Globalization;
using System.Text;
using DatabaseAnalyzer.Core;

namespace DatabaseAnalyzer.App.Reporting;

internal sealed class TextReportRenderer : IReportRenderer
{
    public string RenderReport(AnalysisResult analysisResult)
    {
        var buffer = new StringBuilder();

        if (analysisResult.Issues.Count == 0)
        {
            buffer.AppendLine("No issues found.");
        }
        else
        {
            buffer.AppendLine(CultureInfo.CurrentCulture, $"{analysisResult.Issues.Count} issue(s) found:");
            foreach (var issue in analysisResult.Issues)
            {
                buffer.AppendLine(CultureInfo.CurrentCulture, $"""{issue.DiagnosticDefinition.DiagnosticId} File="{issue.RelativeScriptFilePath}" Issue="{issue.Message}" Location="{issue.CodeRegion}" """);
            }
        }

        if (analysisResult.SuppressedIssues.Count == 0)
        {
            buffer.AppendLine("No suppressed issues found.");
        }
        else
        {
            buffer.AppendLine(CultureInfo.CurrentCulture, $"{analysisResult.SuppressedIssues.Count} suppressed issue(s) found:");
            foreach (var issue in analysisResult.SuppressedIssues)
            {
                buffer.AppendLine(CultureInfo.CurrentCulture, $"    {issue.Issue.DiagnosticDefinition.DiagnosticId} {issue.Issue.RelativeScriptFilePath} {issue.Issue.Message}.    Reason={issue.Reason}");
            }
        }

        return buffer.ToString().Trim();
    }
}
