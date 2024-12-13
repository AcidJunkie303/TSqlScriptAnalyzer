using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts;

public interface IIssueReporter
{
    IReadOnlyList<IIssue> GetIssues();

    void Report(IDiagnosticDefinition rule, string relativeScriptFilePath, string? fullObjectName, CodeRegion codeRegion, params object[] insertionStrings);
}

public static class IssueReporterExtensions
{
    public static void Report(this IIssueReporter issueReporter, IDiagnosticDefinition rule, string relativeScriptFilePath, string? fullObjectName, SqlCodeObject codeObject, params object[] insertionStrings)
        => issueReporter.Report(rule, relativeScriptFilePath, fullObjectName, CodeRegion.From(codeObject), insertionStrings);

    public static void Report(this IIssueReporter issueReporter, IDiagnosticDefinition rule, string relativeScriptFilePath, string? fullObjectName, Token token, params object[] insertionStrings)
        => issueReporter.Report(rule, relativeScriptFilePath, fullObjectName, CodeRegion.From(token), insertionStrings);
}
