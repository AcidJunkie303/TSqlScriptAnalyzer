using DatabaseAnalyzer.App.Reporting.Html;
using DatabaseAnalyzer.App.Reporting.Json;

namespace DatabaseAnalyzer.App.Reporting;

internal static class ReportRendererFactory
{
    public static IReportRenderer Create(ConsoleReportType analysisResult)
        => analysisResult switch
        {
            ConsoleReportType.Text => new TextReportRenderer(),
            ConsoleReportType.Json => new JsonFullReportRenderer(),
            ConsoleReportType.JsonSummary => new JsonMiniReportRenderer(),
            ConsoleReportType.Html => new HtmlReportRenderer(),
            _ => throw new ArgumentOutOfRangeException(nameof(analysisResult), analysisResult, message: null)
        };
}
