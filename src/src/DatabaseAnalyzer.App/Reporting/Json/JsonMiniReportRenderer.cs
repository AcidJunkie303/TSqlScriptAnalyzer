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
            .GroupBy(a => a.DiagnosticDefinition.IssueType.ToString(), StringComparer.Ordinal)
            .ToDictionary(a => a.Key, a => a.Count(), StringComparer.Ordinal);

        var report = new
        {
            TotalIssueCount = totalIssueCount,
            SuppressedIssueCount = suppressedIssueCount,
            IssueCountByType = issueCountByType
        };

        var options = CreateJsonSerializerOptions();
        var renderedReport = JsonSerializer.Serialize(report, options).Trim();
        return Task.FromResult(renderedReport);
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        return new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            WriteIndented = true
        };
    }
}
