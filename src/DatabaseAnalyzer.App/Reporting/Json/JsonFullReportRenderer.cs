using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using DatabaseAnalyzer.App.Reporting.Json.Converters;
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
            analysisResult.Statistics,
            analysisResult.DisabledDiagnostics,
            Issues = analysisResult.Issues
                .OrderBy(static a => a.RelativeScriptFilePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static a => a.CodeRegion)
                .Select(static a => new
                {
                    a.DiagnosticDefinition.DiagnosticId,
                    a.DiagnosticDefinition.Title,
                    a.DiagnosticDefinition.MessageTemplate,
                    a.Message,
                    a.RelativeScriptFilePath,
                    a.CodeRegion,
                    a.DiagnosticDefinition.HelpUrl,
                    a.MessageInsertions
                }),
            SuppressedIssues = analysisResult.SuppressedIssues
                .OrderBy(static a => a.Issue.RelativeScriptFilePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static a => a.Issue.CodeRegion)
                .Select(static a => new
                {
                    a.Issue.DiagnosticDefinition.DiagnosticId,
                    a.Issue.DiagnosticDefinition.Title,
                    a.Issue.DiagnosticDefinition.MessageTemplate,
                    a.Issue.Message,
                    a.Issue.RelativeScriptFilePath,
                    a.Issue.CodeRegion,
                    a.Issue.DiagnosticDefinition.HelpUrl,
                    a.Issue.MessageInsertions,
                    a.Reason
                })
        };

        var options = CreateJsonSerializerOptions();
        var renderedReport = JsonSerializer.Serialize(report, options).Trim();
        return Task.FromResult(renderedReport);
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        options.Converters.Add(new LocationConverter());

        return options;
    }
}
