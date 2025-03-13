using System.Text.Json;
using DatabaseAnalyzer.Core;

namespace DatabaseAnalyzer.App.Reporting.Json;

internal sealed class JsonMiniReportRenderer : IReportRenderer
{
    public Task<string> RenderReportAsync(AnalysisResult analysisResult)
    {
        var totalIssueCount = analysisResult.Issues.Count;
        var suppressedIssueCount = analysisResult.SuppressedIssues.Count;
        var issueCountByType = analysisResult.Issues
            .GroupBy(a => a.DiagnosticDefinition.IssueType.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(a => a.Key, a => a.Count(), StringComparer.OrdinalIgnoreCase);

        var report = new
        {
            TotalIssueCount = totalIssueCount,
            SuppressedIssueCount = suppressedIssueCount,
            IssueCountByType = issueCountByType
        };

        var renderedReport = JsonSerializer.Serialize(report, JsonSerializationOptions.Default).Trim();
        return Task.FromResult(renderedReport);
    }
}
