namespace DatabaseAnalyzer.Common.Contracts;

public interface IIssueReporter
{
    IReadOnlyList<IIssue> Issues { get; }

    void Report(IDiagnosticDefinition rule, string databaseName, string relativeScriptFilePath, string? fullObjectName, CodeRegion codeRegion, params object[] insertionStrings);
}
