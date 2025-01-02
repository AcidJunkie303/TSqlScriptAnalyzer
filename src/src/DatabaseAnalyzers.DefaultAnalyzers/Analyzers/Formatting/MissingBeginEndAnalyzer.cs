using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class MissingBeginEndAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var statement in script.ParsedScript.GetChildren<WhileStatement>(recursive: true))
        {
            Analyze(context, script, statement);
        }

        foreach (var statement in script.ParsedScript.GetChildren<IfStatement>(recursive: true))
        {
            Analyze(context, script, statement.ThenStatement, "IF");
            if (statement.ElseStatement is not null)
            {
                Analyze(context, script, statement.ElseStatement, "ELSE");
            }
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, WhileStatement statement)
    {
        if (statement.Statement is BeginEndBlockStatement)
        {
            return;
        }

        var fullObjectName = statement.TryGetFirstClassObjectName(context, script);
        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, statement.Statement.GetCodeRegion(), "WHILE");
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, TSqlStatement statement, string name)
    {
        if (statement is BeginEndBlockStatement)
        {
            return;
        }

        var fullObjectName = statement.TryGetFirstClassObjectName(context, script);
        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, statement.GetCodeRegion(), name);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5022",
            IssueType.Formatting,
            "Missing BEGIN/END blocks",
            "The children of '{0}' should be enclosed in BEGIN/END blocks."
        );
    }
}
