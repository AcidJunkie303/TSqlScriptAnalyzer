using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class MissingEmptyLineAroundGoStatementAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5045Settings>();
        if (settings is { RequireEmptyLineBeforeGo: false, RequireEmptyLineAfterGo: false })
        {
            return;
        }

        for (var i = 0; i < script.ParsedScript.ScriptTokenStream.Count; i++)
        {
            var token = script.ParsedScript.ScriptTokenStream[i];
            if (token.TokenType != TSqlTokenType.Go)
            {
                continue;
            }

            AnalyzeToken(context, script, settings, token, i);
        }
    }

    private static void AnalyzeToken(IAnalysisContext context, IScriptModel script, Aj5045Settings settings, TSqlParserToken goStatementToken, int tokenIndex)
    {
        var missingBefore = IsMissingEmptyLineBefore();
        var missingAfter = IsMissingEmptyLineAfter();

        if (!missingBefore && !missingAfter)
        {
            return;
        }

        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtLocation(goStatementToken.Line, goStatementToken.Column) ?? DatabaseNames.Unknown;
        var codeRegion = goStatementToken.GetCodeRegion();
        var fullObjectName = script.ParsedScript
            .TryGetSqlFragmentAtPosition(goStatementToken)
            ?.TryGetFirstClassObjectName(context, script);

        if (missingBefore)
        {
            context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, codeRegion, "before");
        }

        if (missingAfter)
        {
            context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, codeRegion, "after");
        }

        bool IsMissingEmptyLineAfter()
        {
            if (tokenIndex == 0 || !settings.RequireEmptyLineAfterGo)
            {
                return false;
            }

            return GetNewLineCountAfterToken() < 2;
        }

        bool IsMissingEmptyLineBefore()
        {
            if (tokenIndex == 0 || !settings.RequireEmptyLineBeforeGo)
            {
                return false;
            }

            return GetNewLineCountBeforeToken() < 2;
        }

        int GetNewLineCountAfterToken()
            => script.ParsedScript.ScriptTokenStream
                .Skip(tokenIndex + 1)
                .TakeWhile(t => t.TokenType == TSqlTokenType.WhiteSpace)
                .Sum(a => a.Text.Count(c => c == '\n'));

        int GetNewLineCountBeforeToken()
            => script.ParsedScript.ScriptTokenStream
                .Take(tokenIndex)
                .Reverse()
                .TakeWhile(t => t.TokenType == TSqlTokenType.WhiteSpace)
                .Sum(a => a.Text.Count(c => c == '\n'));
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5045",
            IssueType.Formatting,
            "Missing empty line before/after GO batch separators",
            "Missing empty line {0} GO statement.",
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
