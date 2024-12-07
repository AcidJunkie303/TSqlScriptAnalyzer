namespace DatabaseAnalyzer.Contracts;

public interface IIssueReporter
{
    IReadOnlyList<IIssue> GetReportedIssues();

    void Report(IDiagnosticDefinition rule, string fullScriptFilePath, string? fullObjectName, SourceSpan codeRegion, params IReadOnlyList<string> insertionStrings);
}
