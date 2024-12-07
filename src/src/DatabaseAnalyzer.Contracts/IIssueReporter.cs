namespace DatabaseAnalyzer.Contracts;

public interface IIssueReporter
{
    IReadOnlyList<IIssue> ReportedIssues { get; }

    void ReportIssue(IDiagnosticDefinition rule, string fullScriptFilePath, string? fullObjectName, SourceSpan codeRegion, params IReadOnlyList<string> insertionStrings);
}
