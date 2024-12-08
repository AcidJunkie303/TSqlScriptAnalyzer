using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts;

public interface IIssueReporter
{
    IReadOnlyList<IIssue> GetReportedIssues();

    void Report(IDiagnosticDefinition rule, string fullScriptFilePath, string? fullObjectName, SourceSpan codeRegion, params IReadOnlyList<string> insertionStrings);
}

public static class IssueReporterExtensions
{
    public static void Report(this IIssueReporter issueReporter, IDiagnosticDefinition rule, string fullScriptFilePath, string? fullObjectName, SqlCodeObject codeObject, params IReadOnlyList<string> insertionStrings)
        => issueReporter.Report(rule, fullScriptFilePath, fullObjectName, SourceSpan.From(codeObject), insertionStrings);

    public static void Report(this IIssueReporter issueReporter, IDiagnosticDefinition rule, string fullScriptFilePath, string? fullObjectName, Token token, params IReadOnlyList<string> insertionStrings)
        => issueReporter.Report(rule, fullScriptFilePath, fullObjectName, SourceSpan.From(token), insertionStrings);
}
