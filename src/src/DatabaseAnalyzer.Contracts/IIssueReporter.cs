using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts;

public interface IIssueReporter
{
    IReadOnlyList<IIssue> GetReportedIssues();

    void Report(IDiagnosticDefinition rule, string fullScriptFilePath, string? fullObjectName, CodeRegion codeRegion, params object[] insertionStrings);
}

public static class IssueReporterExtensions
{
    public static void Report(this IIssueReporter issueReporter, IDiagnosticDefinition rule, string fullScriptFilePath, string? fullObjectName, SqlCodeObject codeObject, params object[] insertionStrings)
        => issueReporter.Report(rule, fullScriptFilePath, fullObjectName, CodeRegion.From(codeObject), insertionStrings);

    public static void Report(this IIssueReporter issueReporter, IDiagnosticDefinition rule, string fullScriptFilePath, string? fullObjectName, Token token, params object[] insertionStrings)
        => issueReporter.Report(rule, fullScriptFilePath, fullObjectName, CodeRegion.From(token), insertionStrings);
}
