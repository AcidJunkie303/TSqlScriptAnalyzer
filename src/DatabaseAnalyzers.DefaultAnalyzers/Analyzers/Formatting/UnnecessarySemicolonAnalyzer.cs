using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class UnnecessarySemicolonAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public UnnecessarySemicolonAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript()
    {
        for (var i = 0; i < _script.ParsedScript.ScriptTokenStream.Count; i++)
        {
            var token = _script.ParsedScript.ScriptTokenStream[i];
            if (token.TokenType != TSqlTokenType.Semicolon)
            {
                continue;
            }

            Analyze(token, i);
        }
    }

    private void Analyze(TSqlParserToken semicolonToken, int tokenIndex)
    {
        if (IsSemicolonRequired(tokenIndex))
        {
            return;
        }

        var fragment = _script.ParsedScript.TryGetSqlFragmentAtPosition(semicolonToken.Line, semicolonToken.Column);
        var fullObjectName = fragment?.TryGetFirstClassObjectName(_context, _script);
        var databaseName = fragment is null
            ? _script.DatabaseName
            : _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragment) ?? _script.DatabaseName;
        var codeRegion = CodeRegion.Create(semicolonToken.Line, semicolonToken.Column, semicolonToken.Line, semicolonToken.Column + 1);
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, codeRegion);
    }

    private bool IsSemicolonRequired(int semicolonTokenIndex)
    {
        if (semicolonTokenIndex == 0)
        {
            return false;
        }

        if (IsSemicolonRequiredForNextStatement(_script.ParsedScript.ScriptTokenStream, semicolonTokenIndex, _script.ParsedScript.LastTokenIndex))
        {
            return true;
        }

        if (IsSemicolonRequiredForPreviousStatement(semicolonTokenIndex))
        {
            return true;
        }

        return false;
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

            return token.TokenType == TSqlTokenType.With;
        }

        return false;
    }

    private bool IsSemicolonRequiredForPreviousStatement(int semicolonTokenIndex)
    {
        // check tokens before this one for 'THROW' and 'MERGE'
        for (var i = semicolonTokenIndex - 1; i >= 0; i--)
        {
            var token = _script.ParsedScript.ScriptTokenStream[i];
            if (token.TokenType is TSqlTokenType.MultilineComment or TSqlTokenType.SingleLineComment or TSqlTokenType.WhiteSpace)
            {
                continue;
            }

            var fragment = _script.ParsedScript.TryGetSqlFragmentAtPosition(token.Line, token.Column);
            var parentStatement = fragment?.GetParents(_script.ParentFragmentProvider)
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
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
