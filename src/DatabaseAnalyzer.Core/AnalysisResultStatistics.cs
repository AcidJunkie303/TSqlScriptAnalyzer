namespace DatabaseAnalyzer.Core;

public sealed record AnalysisResultStatistics(
    int TotalDisabledDiagnosticCount,
    int TotalErrorCount,
    int TotalFormattingIssueCount,
    int TotalInformationIssueCount,
    int TotalIssueCount,
    int TotalMissingIndexIssueCount,
    int TotalSuppressedIssueCount,
    int TotalWarningCount,
    int TotalScripts,
    TimeSpan AnalysisDuration
);
