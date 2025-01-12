using System.Text.Json;
using DatabaseAnalyzer.Core;

namespace DatabaseAnalyzer.App.Reporting.Json;

internal sealed class JsonFullReportRenderer : IReportRenderer
{
    public Task<string> RenderReportAsync(AnalysisResult analysisResult)
    {
        var report = new
        {
            analysisResult.DisabledDiagnostics,
            Issues = analysisResult.Issues
                .OrderBy(static a => a.RelativeScriptFilePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static a => a.CodeRegion)
                .Select(static a => new
                {
                    a.DiagnosticDefinition.DiagnosticId,
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
