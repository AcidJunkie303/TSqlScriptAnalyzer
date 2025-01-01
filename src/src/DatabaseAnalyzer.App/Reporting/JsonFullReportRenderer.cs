using System.Text.Encodings.Web;
using System.Text.Json;
using DatabaseAnalyzer.Core;

namespace DatabaseAnalyzer.App.Reporting;

internal sealed class JsonFullReportRenderer : IReportRenderer
{
    public string RenderReport(AnalysisResult analysisResult)
    {
        var report = new
        {
            analysisResult.DisabledDiagnostics,
            Issues = analysisResult.Issues.Select(static a => new
            {
                a.DiagnosticDefinition.DiagnosticId,
                a.Message,
                a.RelativeScriptFilePath,
                a.CodeRegion
            }),
            SuppressedIssues = analysisResult.SuppressedIssues.Select(static a => new
            {
                a.Issue.DiagnosticDefinition.DiagnosticId,
                a.Issue.Message,
                a.Reason,
                a.Issue.RelativeScriptFilePath,
                a.Issue.CodeRegion
            })
        };

        var options = CreateJsonSerializerOptions();
        return JsonSerializer.Serialize(report, options).Trim();
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        return new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}
