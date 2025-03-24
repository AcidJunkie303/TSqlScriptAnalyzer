using System.Collections.Frozen;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class MissingBlankSpaceAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public MissingBlankSpaceAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        var tokens = _script.ParsedScript.ScriptTokenStream;

        // we skip the first and last since it doesn't make sense to check them, and it also makes the checking easier (out of bounds checking)
        for (var i = 1; i < tokens.Count - 1; i++)
        {
            var token = tokens[i];
            if (RequiresSpaceBefore(token, i, tokens))
            {
                var previousToken = tokens[i - 1];
                if (previousToken.TokenType != TSqlTokenType.WhiteSpace)
                {
                    Report(token, "before");
                }
            }

            if (RequiresSpaceAfter(token, i, tokens))
            {
                var nextToken = tokens[i + 1];
                if (nextToken.TokenType != TSqlTokenType.WhiteSpace)
                {
                    Report(token, "after");
                }
            }
        }

        void Report(TSqlParserToken token, string beforeOrAfter)
        {
            var fullObjectName = _script.ParsedScript
                .TryGetSqlFragmentAtPosition(token)
                ?.TryGetFirstClassObjectName(_context, _script);

            var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtToken(token) ?? DatabaseNames.Unknown;
            _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, token.GetCodeRegion(), beforeOrAfter, token.Text);
        }
    }

    private static bool RequiresSpaceBefore(TSqlParserToken token, int tokenIndex, IList<TSqlParserToken> tokens)
    {
        var previousToken = tokens[tokenIndex - 1];
        if (previousToken.TokenType is TSqlTokenType.WhiteSpace)
        {
            return false;
        }

        if (token.TokenType is TSqlTokenType.EqualsSign)
        {
            return previousToken.TokenType != TSqlTokenType.LessThan && previousToken.TokenType != TSqlTokenType.GreaterThan;
        }

        if (token.TokenType is TSqlTokenType.GreaterThan)
        {
            return previousToken.TokenType != TSqlTokenType.LessThan;
        }

        return Sets.TokenTypesWhichRequireSpaceBeforeAndAfter.Contains(token.TokenType);
    }

    private static bool RequiresSpaceAfter(TSqlParserToken token, int tokenIndex, IList<TSqlParserToken> tokens)
    {
        if (!Sets.TokenTypesWhichRequireSpaceBeforeAndAfter.Contains(token.TokenType)
            && !Sets.TokenTypesWhichRequireSpaceAfter.Contains(token.TokenType))
        {
            return false;
        }

        if (token.TokenType == TSqlTokenType.Minus)
        {
            var previousNonWhiteSpaceToken = GetPreviousToken(tokens, tokenIndex, static a => a.TokenType == TSqlTokenType.WhiteSpace);
            if (previousNonWhiteSpaceToken is null)
            {
                return false;
            }

            return previousNonWhiteSpaceToken.TokenType
                is not (TSqlTokenType.GreaterThan
                or TSqlTokenType.LessThan
                or TSqlTokenType.EqualsSign);
        }

        if (token.TokenType == TSqlTokenType.LessThan)
        {
            var nextToken = tokens[tokenIndex + 1];
            return nextToken.TokenType != TSqlTokenType.EqualsSign && nextToken.TokenType != TSqlTokenType.GreaterThan;
        }

        if (token.TokenType == TSqlTokenType.GreaterThan)
        {
            var nextToken = tokens[tokenIndex + 1];
            return nextToken.TokenType != TSqlTokenType.EqualsSign;
        }

        return true;
    }

    private static TSqlParserToken? GetPreviousToken(IList<TSqlParserToken> tokens, int index, Func<TSqlParserToken, bool> skipWhile)
        => tokens
            .Take(index)
            .Reverse()
            .SkipWhile(skipWhile)
            .FirstOrDefault();

    private static class Sets
    {
        public static readonly FrozenSet<TSqlTokenType> TokenTypesWhichRequireSpaceBeforeAndAfter = new[]
            {
                TSqlTokenType.Plus,
                TSqlTokenType.Minus,
                TSqlTokenType.Star,
                TSqlTokenType.Divide,
                TSqlTokenType.TSEqual,
                TSqlTokenType.MultiplyEquals,
                TSqlTokenType.EqualsSign,
                TSqlTokenType.AddEquals,
                TSqlTokenType.SubtractEquals,
                TSqlTokenType.DivideEquals,
                TSqlTokenType.ModEquals,
                TSqlTokenType.BitwiseAndEquals,
                TSqlTokenType.BitwiseOrEquals,
                TSqlTokenType.BitwiseXorEquals,
                TSqlTokenType.ConcatEquals,
                TSqlTokenType.PercentSign,
                TSqlTokenType.LessThan,
                TSqlTokenType.GreaterThan,
                TSqlTokenType.Tilde,
                TSqlTokenType.LeftShift,
                TSqlTokenType.RightShift
            }
            .ToFrozenSet();

        public static readonly FrozenSet<TSqlTokenType> TokenTypesWhichRequireSpaceAfter = new[]
            {
                TSqlTokenType.Comma
            }
            .ToFrozenSet();
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5010",
            IssueType.Formatting,
            "Missing blank-space",
            "Missing blank-space {0} `{1}`",
            ["Before/after", "Statement"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
