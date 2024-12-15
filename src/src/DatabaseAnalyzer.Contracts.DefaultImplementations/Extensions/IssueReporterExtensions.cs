using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class IssueReporterExtensions
{
    public static void Report(this IIssueReporter issueReporter, IDiagnosticDefinition rule, IScriptModel script, string? fullObjectName, CodeRegion codeRegion, params object[] insertionStrings)
        => issueReporter.Report(rule, script.DatabaseName, script.RelativeScriptFilePath, fullObjectName, codeRegion, insertionStrings);

    public static void Report(this IIssueReporter issueReporter, IDiagnosticDefinition rule, IScriptModel script, string? fullObjectName, TSqlFragment fragment, params object[] insertionStrings)
        => issueReporter.Report(rule, script.DatabaseName, script.RelativeScriptFilePath, fullObjectName, fragment.GetCodeRegion(), insertionStrings);

    public static void Report(this IIssueReporter issueReporter, IDiagnosticDefinition rule, IScriptModel script, string? fullObjectName, TSqlParserToken token, params object[] insertionStrings)
        => issueReporter.Report(rule, script.DatabaseName, script.RelativeScriptFilePath, fullObjectName, token.GetCodeRegion(), insertionStrings);
}
