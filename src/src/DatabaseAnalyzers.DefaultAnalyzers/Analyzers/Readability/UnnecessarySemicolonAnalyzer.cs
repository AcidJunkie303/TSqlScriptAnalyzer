using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Readability;

public sealed class UnnecessarySemicolonAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        for (var i = 0; i < script.ParsedScript.ScriptTokenStream.Count; i++)
        {
            var token = script.ParsedScript.ScriptTokenStream[i];
            if (token.TokenType != TSqlTokenType.Semicolon)
            {
                continue;
            }

            Analyze(context, script, token, i);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, TSqlParserToken semicolonToken, int tokenIndex)
    {
        if (IsSemicolonRequired(script, tokenIndex))
        {
            return;
        }

        var fragment = script.ParsedScript.TryGetSqlFragmentAtPosition(semicolonToken.Line, semicolonToken.Column);
        var fullObjectName = fragment?.TryGetFirstClassObjectName(context, script);
        var databaseName = fragment?.FindCurrentDatabaseNameAtFragment(script.ParsedScript) ?? script.DatabaseName;
        var codeRegion = new CodeRegion(semicolonToken.Line, semicolonToken.Column, semicolonToken.Line, semicolonToken.Column + 1);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, codeRegion);

        static bool IsSemicolonRequired(IScriptModel script, int tokenIndex)
        {
            if (tokenIndex == 0)
            {
                return false;
            }

            // check tokens after this one for CTEs (WITH)
            for (var i = tokenIndex + 1; i <= script.ParsedScript.LastTokenIndex; i++)
            {
                var token = script.ParsedScript.ScriptTokenStream[i];
                if (token.TokenType is TSqlTokenType.MultilineComment or TSqlTokenType.SingleLineComment or TSqlTokenType.WhiteSpace)
                {
                    continue;
                }

                if (token.TokenType == TSqlTokenType.With)
                {
                    return true;
                }
            }

            // check tokens before this one for 'THROW' and 'MERGE'
            for (var i = tokenIndex - 1; i >= 0; i--)
            {
                var token = script.ParsedScript.ScriptTokenStream[i];
                if (token.TokenType is TSqlTokenType.MultilineComment or TSqlTokenType.SingleLineComment or TSqlTokenType.WhiteSpace)
                {
                    continue;
                }

                var fragment = script.ParsedScript.TryGetSqlFragmentAtPosition(token.Line, token.Column);
                if (fragment is null)
                {
                    return true; // to be on the safe side
                }

                var parentStatement = fragment
                    .GetParents(script.ParentFragmentProvider)
                    .OfType<TSqlStatement>()
                    .FirstOrDefault();
                if (parentStatement is null)
                {
                    return true; // to be on the safe side
                }

                return parentStatement is MergeStatement or ThrowStatement; // merge statements must be terminated with a semicolon. throw statements should be terminated with a semicolon.
            }

            return true;
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5028",
            IssueType.Warning,
            "Semicolon is not necessary",
            "Semicolon is not necessary."
        );
    }
}
