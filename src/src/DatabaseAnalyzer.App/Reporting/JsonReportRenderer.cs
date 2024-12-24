using System.Text.Json;
using DatabaseAnalyzer.Core;

namespace DatabaseAnalyzer.App.Reporting;

internal sealed class JsonReportRenderer : IReportRenderer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public string RenderReport(AnalysisResult analysisResult)
    {
        var report = new
        {
            analysisResult.DisabledDiagnostics,
            Issues = analysisResult.Issues.Select(a => new
            {
                a.DiagnosticDefinition.DiagnosticId,
                a.Message,
                a.RelativeScriptFilePath,
                a.CodeRegion
            }),
            SuppressedIssues = analysisResult.SuppressedIssues.Select(a => new
            {
                a.Issue.DiagnosticDefinition.DiagnosticId,
                a.Issue.Message,
                a.Reason,
                a.Issue.RelativeScriptFilePath,
                a.Issue.CodeRegion
            })
        };

        return JsonSerializer.Serialize(report, Options).Trim();
    }
}
