using System.Collections.Frozen;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class UnnecessarySemicolonAnalyzer : IScriptAnalyzer
{
    private static readonly FrozenSet<TSqlTokenType> SkipTokens = new[]
    {
        TSqlTokenType.WhiteSpace,
        TSqlTokenType.MultilineComment,
        TSqlTokenType.SingleLineComment,
        TSqlTokenType.EndOfFile
    }.ToFrozenSet();

    private static readonly FrozenSet<string> TokensContentsWhichRequirePrecedingSemiColon = new[]
    {
        "THROW",
        "WITH"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;
    private readonly IList<TSqlParserToken> _tokens;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public UnnecessarySemicolonAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
        _tokens = context.Script.ParsedScript.ScriptTokenStream;
    }

    public void AnalyzeScript()
    {
        for (var i = 0; i < _tokens.Count; i++)
        {
            var token = _tokens[i];
            if (token.TokenType != TSqlTokenType.Semicolon)
            {
                continue;
            }

            Analyze(i);
        }
    }

    private void Analyze(int tokenIndex)
    {
        if (DoesNextStatementRequirePrecedingSemiColon(tokenIndex))
        {
            return;
        }

        if (DoesPreviousStatementRequirePrecedingSemiColon(tokenIndex))
        {
            return;
        }

        var token = _tokens[tokenIndex];
        var fragment = _script.ParsedScript.TryGetSqlFragmentAtPosition(token.Line, token.Column);
        var fullObjectName = fragment?.TryGetFirstClassObjectName(_context, _script);
        var databaseName = fragment is null
            ? _script.DatabaseName
            : _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragment) ?? _script.DatabaseName;
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, token.GetCodeRegion());
    }

    private bool DoesNextStatementRequirePrecedingSemiColon(int tokenIndex)
    {
        foreach (var token in _tokens.Skip(tokenIndex + 1))
        {
            if (SkipTokens.Contains(token.TokenType))
            {
                continue;
            }

            return TokensContentsWhichRequirePrecedingSemiColon.Contains(token.Text);
        }

        return false;
    }

    private bool DoesPreviousStatementRequirePrecedingSemiColon(int tokenIndex)
    {
        foreach (var token in _tokens.Take(tokenIndex - 1).Reverse())
        {
            if (SkipTokens.Contains(token.TokenType))
            {
                continue;
            }

            var tokenLocation = token.GetCodeLocation();
            var surroundingFragments = _script.ParsedScript
                .GetChildren(recursive: true)
                .Where(a => tokenLocation.IsInside(a.GetCodeRegion()));

            return surroundingFragments.OfType<MergeStatement>().Any();
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
