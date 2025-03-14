using DatabaseAnalyzer.Core;
using Razor.Templating.Core;

namespace DatabaseAnalyzer.App.Reporting.Html;

internal sealed class HtmlReportRenderer : IReportRenderer
{
    private readonly ReportTheme _theme;

    public HtmlReportRenderer(ReportTheme theme)
    {
        _theme = theme;
    }

    public Task<string> RenderReportAsync(AnalysisResult analysisResult)
    {
        var viewBag = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            { "Theme", _theme }
        };
        return RazorTemplateEngine.RenderAsync("Reporting/Html/HtmlReport.cshtml", viewModel: analysisResult, viewBagOrViewData: viewBag);
    }
}
