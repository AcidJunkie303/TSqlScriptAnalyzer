using System.Text;
using DatabaseAnalyzer.Core;

namespace DatabaseAnalyzer.App.Reporting;

internal static class ReportFileRenderer
{
    public static void Render(IReportRenderer renderer, AnalysisResult analysisResult, string filePath, Encoding? encoding = null)
    {
        var directoryPath = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException("Unable to determine directory path");
        if (directoryPath.Length > 0 && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var report = renderer.RenderReport(analysisResult);
        File.WriteAllText(filePath, report, encoding ?? Encoding.UTF8);
    }
}
