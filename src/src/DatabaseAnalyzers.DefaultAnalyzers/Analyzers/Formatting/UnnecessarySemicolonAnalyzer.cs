using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

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
        var databaseName = fragment is null
            ? script.DatabaseName
            : script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragment) ?? script.DatabaseName;
        var codeRegion = CodeRegion.Create(semicolonToken.Line, semicolonToken.Column, semicolonToken.Line, semicolonToken.Column + 1);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, codeRegion);

        static bool IsSemicolonRequired(IScriptModel script, int semicolonTokenIndex)
        {
            if (semicolonTokenIndex == 0)
            {
                return false;
            }

            if (IsSemicolonRequiredForNextStatement(script.ParsedScript.ScriptTokenStream, semicolonTokenIndex, script.ParsedScript.LastTokenIndex))
            {
                return true;
            }

            if (IsSemicolonRequiredForPreviousStatement(script, semicolonTokenIndex))
            {
                return true;
            }

            return false;
        }
    }

    private static bool IsSemicolonRequiredForNextStatement(IList<TSqlParserToken> tokens, int semicolonTokenIndex, int lastTokenIndex)
    {
        // check tokens after this one for CTEs (WITH)
        for (var i = semicolonTokenIndex + 1; i <= lastTokenIndex; i++)
        {
            var token = tokens[i];
            if (token.TokenType is TSqlTokenType.MultilineComment or TSqlTokenType.SingleLineComment or TSqlTokenType.WhiteSpace)
            {
                continue;
            }

            if (token.TokenType == TSqlTokenType.With)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSemicolonRequiredForPreviousStatement(IScriptModel script, int semicolonTokenIndex)
    {
        // check tokens before this one for 'THROW' and 'MERGE'
        for (var i = semicolonTokenIndex - 1; i >= 0; i--)
        {
            var token = script.ParsedScript.ScriptTokenStream[i];
            if (token.TokenType is TSqlTokenType.MultilineComment or TSqlTokenType.SingleLineComment or TSqlTokenType.WhiteSpace)
            {
                continue;
            }

            var fragment = script.ParsedScript.TryGetSqlFragmentAtPosition(token.Line, token.Column);
            var parentStatement = fragment?.GetParents(script.ParentFragmentProvider)
                .OfType<TSqlStatement>()
                .FirstOrDefault();

            if (parentStatement is null)
            {
                return true; // to be on the safe side
            }

            return parentStatement is MergeStatement or ThrowStatement; // merge statements must be terminated with a semicolon. throw statements should be terminated with a semicolon.
        }

        return false;
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5028",
            IssueType.Formatting,
            "Semicolon is not necessary",
            "Semicolon is not necessary.",
            [],
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
