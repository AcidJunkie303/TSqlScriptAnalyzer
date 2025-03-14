using DatabaseAnalyzer.Core;

namespace DatabaseAnalyzer.App.Reporting;

internal interface IReportRenderer
{
    Task<string> RenderReportAsync(AnalysisResult analysisResult);
}
