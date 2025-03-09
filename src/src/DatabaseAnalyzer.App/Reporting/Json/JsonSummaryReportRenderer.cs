using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using DatabaseAnalyzer.Core;

namespace DatabaseAnalyzer.App.Reporting.Json;

internal sealed class JsonSummaryReportRenderer : IReportRenderer
{
    [SuppressMessage("Major Code Smell", "S6354:Use a testable date/time provider")]
    public Task<string> RenderReportAsync(AnalysisResult analysisResult)
    {
        var totalIssueCount = analysisResult.Issues.Count;
        var suppressedIssueCount = analysisResult.SuppressedIssues.Count;
        var issueCountByType = analysisResult.Issues
            .GroupBy(a => a.DiagnosticDefinition.IssueType.ToString(), StringComparer.Ordinal)
            .ToDictionary(a => a.Key, a => a.Count(), StringComparer.Ordinal);

        var report = new
        {
            CreatedAt = DateTimeOffset.UtcNow,
            TotalIssueCount = totalIssueCount,
            SuppressedIssueCount = suppressedIssueCount,
            IssueCountByType = issueCountByType
        };

        var renderedReport = JsonSerializer.Serialize(report, JsonSerializationOptions.Default).Trim();
        return Task.FromResult(renderedReport);
    }
}
