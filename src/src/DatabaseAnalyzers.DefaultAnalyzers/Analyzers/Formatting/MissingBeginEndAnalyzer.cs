using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class MissingBeginEndAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5022Settings>();

        if (settings.WhileRequiresBeginEndBlock)
        {
            foreach (var statement in script.ParsedScript.GetChildren<WhileStatement>(recursive: true))
            {
                AnalyzeWhileStatement(context, script, statement);
            }
        }

        if (settings.IfRequiresBeginEndBlock)
        {
            foreach (var statement in script.ParsedScript.GetChildren<IfStatement>(recursive: true))
            {
                AnalyzeIfStatement(context, script, statement.ThenStatement, "IF");
                if (statement.ElseStatement is not null)
                {
                    AnalyzeIfStatement(context, script, statement.ElseStatement, "ELSE");
                }
            }
        }
    }

    private static void AnalyzeWhileStatement(IAnalysisContext context, IScriptModel script, WhileStatement statement)
    {
        if (statement.Statement is BeginEndBlockStatement)
        {
            return;
        }

        Report(context, script, statement.Statement, "WHILE");
    }

    private static void AnalyzeIfStatement(IAnalysisContext context, IScriptModel script, TSqlStatement statement, string statementName)
    {
        if (statement is BeginEndBlockStatement)
        {
            return;
        }

        Report(context, script, statement, statementName);
    }

    private static void Report(IAnalysisContext context, IScriptModel script, TSqlFragment fragmentToReport, string statementName)
    {
        var fullObjectName = fragmentToReport.TryGetFirstClassObjectName(context, script);
        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragmentToReport) ?? DatabaseNames.Unknown;
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, fragmentToReport.GetCodeRegion(), statementName);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5022",
            IssueType.Formatting,
            "Missing BEGIN/END blocks",
            "The children of '{0}' should be enclosed in BEGIN/END blocks.",
            ["Statement name"],
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
