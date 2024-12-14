namespace DatabaseAnalyzer.App.Reporting;

internal static class ReportRendererFactory
{
    public static IReportRenderer Create(ConsoleReportType analysisResult)
        => analysisResult switch
        {
            ConsoleReportType.Text => new TextReportRenderer(),
            ConsoleReportType.Json => new JsonReportRenderer(),
            _ => throw new ArgumentOutOfRangeException(nameof(analysisResult), analysisResult, null)
        };
}
