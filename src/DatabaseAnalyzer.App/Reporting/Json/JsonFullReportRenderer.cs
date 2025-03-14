using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using DatabaseAnalyzer.Core;

namespace DatabaseAnalyzer.App.Reporting.Json;

internal sealed class JsonFullReportRenderer : IReportRenderer
{
    [SuppressMessage("Major Code Smell", "S6354:Use a testable date/time provider")]
    public Task<string> RenderReportAsync(AnalysisResult analysisResult)
    {
        var report = new
        {
            CreatedAt = DateTimeOffset.UtcNow,
            analysisResult.DisabledDiagnostics,
            Issues = analysisResult.Issues
                .OrderBy(static a => a.RelativeScriptFilePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static a => a.CodeRegion)
                .Select(static a => new
                {
                    a.DiagnosticDefinition.DiagnosticId,
                    a.DiagnosticDefinition.Title,
                    a.Message,
                    a.RelativeScriptFilePath,
                    a.CodeRegion,
                    a.DiagnosticDefinition.HelpUrl
                }),
            SuppressedIssues = analysisResult.SuppressedIssues
                .OrderBy(static a => a.Issue.RelativeScriptFilePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static a => a.Issue.CodeRegion)
                .Select(static a => new
                {
                    a.Issue.DiagnosticDefinition.DiagnosticId,
                    a.Issue.Message,
                    a.Reason,
                    a.Issue.RelativeScriptFilePath,
                    a.Issue.CodeRegion,
                    a.Issue.DiagnosticDefinition.HelpUrl
                })
        };

        var renderedReport = JsonSerializer.Serialize(report, JsonSerializationOptions.Default).Trim();
        return Task.FromResult(renderedReport);
    }
}
