namespace DatabaseAnalyzer.Contracts;

public interface IIssueReporter
{
    IReadOnlyList<IIssue> GetIssues();

    void Report(IDiagnosticDefinition rule, string databaseName, string relativeScriptFilePath, string? fullObjectName, CodeRegion codeRegion, params object[] insertionStrings);
}
