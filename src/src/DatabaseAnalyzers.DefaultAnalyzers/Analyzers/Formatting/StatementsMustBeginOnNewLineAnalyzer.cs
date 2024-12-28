using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class StatementsMustBeginOnNewLineAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsRetriever.GetSettings<Aj5023Settings>();

        foreach (var statement in script.ParsedScript.GetChildren<TSqlStatement>(true))
        {
            Analyze(context, script, settings, statement);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, Aj5023Settings settings, TSqlStatement statement)
    {
        if (statement.FirstTokenIndex == 0)
        {
            return; // nothing to check here since this is the first token
        }

        var statementToken = statement.ScriptTokenStream[statement.FirstTokenIndex];
        if (settings.StatementTypesToIgnore.Contains(statementToken.TokenType))
        {
            return;
        }

        for (var i = statement.FirstTokenIndex - 1; i >= 0; i--)
        {
            var token = statement.ScriptTokenStream[i];

            if (token.TokenType == TSqlTokenType.WhiteSpace)
            {
                if (token.Text.Contains('\n', StringComparison.Ordinal))
                {
                    return;
                }

                continue;
            }

            var fullObjectName = statement.TryGetFirstClassObjectName(context, script);
            var databaseName = statement.FindCurrentDatabaseNameAtFragment(script.ParsedScript);
            context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, statement.GetCodeRegion());

            return;
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5023",
            IssueType.Formatting,
            "Statements must begin on a new line",
            "Statements must begin on a new line."
        );
    }
}
