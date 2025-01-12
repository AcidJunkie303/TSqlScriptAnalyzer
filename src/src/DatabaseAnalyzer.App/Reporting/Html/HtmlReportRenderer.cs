using DatabaseAnalyzer.Core;
using Razor.Templating.Core;

namespace DatabaseAnalyzer.App.Reporting.Html;

internal sealed class HtmlReportRenderer : IReportRenderer
{
    public Task<string> RenderReportAsync(AnalysisResult analysisResult) => RazorTemplateEngine.RenderAsync("Reporting/Html/HtmlReport.cshtml", analysisResult);
}
