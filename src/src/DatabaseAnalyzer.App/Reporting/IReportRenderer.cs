using DatabaseAnalyzer.Core;

namespace DatabaseAnalyzer.App.Reporting;

internal interface IReportRenderer
{
    string RenderReport(AnalysisResult analysisResult);
}
