using DatabaseAnalyzer.App.Reporting.Html;
using DatabaseAnalyzer.App.Reporting.Json;

namespace DatabaseAnalyzer.App.Reporting;

internal static class ReportRendererFactory
{
    public static IReportRenderer Create(ReportType analysisResult, ReportTheme reportTheme)
        => analysisResult switch
        {
            ReportType.Text        => new TextReportRenderer(),
            ReportType.Json        => new JsonFullReportRenderer(),
            ReportType.JsonSummary => new JsonSummaryReportRenderer(),
            ReportType.Html        => new HtmlReportRenderer(reportTheme),
            _                      => throw new ArgumentOutOfRangeException(nameof(analysisResult), analysisResult, message: null)
        };
}
