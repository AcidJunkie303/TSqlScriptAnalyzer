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
            .GroupBy(static a => a.DiagnosticDefinition.IssueType.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static a => a.Key,
                static a => a.Count(),
                StringComparer.OrdinalIgnoreCase);

        var report = new
        {
            CreatedAt = DateTimeOffset.UtcNow,
            analysisResult.Statistics.AnalysisDuration,
            TotalIssueCount = totalIssueCount,
            SuppressedIssueCount = suppressedIssueCount,
            IssueCountByType = issueCountByType
        };

        var renderedReport = JsonSerializer.Serialize(report, JsonSerializationOptions.Default).Trim();
        return Task.FromResult(renderedReport);
    }
}
