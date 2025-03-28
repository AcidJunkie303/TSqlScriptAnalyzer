using System.Collections.Frozen;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

// TODO: remove
#pragma warning disable

internal static class MissingBlankSpaceAnalyzerEvaluators
{
    public static bool IsSpaceRequiredBeforeEqualSign(IList<TSqlParserToken> tokens, int currentTokenIndex)
    {
        var previousToken = tokens.GetPreviousToken(currentTokenIndex);
        if (previousToken is null)
        {
            return false;
        }

        return previousToken.TokenType != TSqlTokenType.GreaterThan
               && previousToken.TokenType != TSqlTokenType.LessThan
               && previousToken.TokenType != TSqlTokenType.Bang; // exclamation mark
    }

    public static bool IsSpaceRequiredAfterEqualSign(IList<TSqlParserToken> tokens, int currentTokenIndex)
    {
        var previousToken = tokens.GetPreviousToken(currentTokenIndex);
        if (previousToken is null)
        {
            return false;
        }

        return previousToken.TokenType != TSqlTokenType.GreaterThan
               && previousToken.TokenType != TSqlTokenType.LessThan
               && previousToken.TokenType != TSqlTokenType.Bang; // exclamation mark
    }
}

public sealed class MissingBlankSpaceAnalyzer : IScriptAnalyzer
{
    private static readonly IsSpaceRequired SpaceIsNeverRequired = (_, _) => false;
    private static readonly IsSpaceRequired SpaceIsAlwaysRequired = (_, _) => true;

    private static readonly FrozenDictionary<TSqlTokenType, IsSpaceRequired> EvaluatorsForPrecedingWhiteSpaceTokenByTokenType = new Dictionary<TSqlTokenType, IsSpaceRequired>
    {
        { TSqlTokenType.Comma, SpaceIsNeverRequired },
        { TSqlTokenType.EqualsSign, MissingBlankSpaceAnalyzerEvaluators.IsSpaceRequiredBeforeEqualSign }
    }.ToFrozenDictionary();

    private static FrozenDictionary<TSqlTokenType, IsSpaceRequired> EvaluatorsForTrailingWhiteSpaceTokenByTokenType = new Dictionary<TSqlTokenType, IsSpaceRequired>
    {
        { TSqlTokenType.Comma, SpaceIsAlwaysRequired },
        { TSqlTokenType.EqualsSign, MissingBlankSpaceAnalyzerEvaluators.IsSpaceRequiredAfterEqualSign }
    }.ToFrozenDictionary();

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

        var fragmentsByLocation = _script.ParsedScript
            .GetChildren(recursive: true)
            .GroupBy(a => a.GetCodeLocation())
            .ToDictionary(a => a.Key, a => a.ToList().AsReadOnly());

        // TODO: remoev
        Console.WriteLine(Sets.TokenTypesWhichRequireSpaceAfter);
        Console.WriteLine(Sets.TokenTypesWhichRequireSpaceBefore);

        for (var i = 1; i < tokens.Count - 1; i++)
        {
            var token = tokens[i];
            if (Sets.TokenTypesWhichRequireSpaceBefore.Contains(token.TokenType))
            {
                var evaluator = EvaluatorsForPrecedingWhiteSpaceTokenByTokenType.GetValueOrDefault(token.TokenType)
                                ?? SpaceIsNeverRequired;
                evaluator(tokens, i)
            }
        }

        /*
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
        */
    }

    private delegate bool IsSpaceRequired(IList<TSqlParserToken> tokens, int currentTokenIndex);

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

    private static class Sets
    {
        public static readonly FrozenDictionary<TSqlTokenType, IsSpaceRequired> SpaceBeforeRequiredEvaluatorByTokenType = new Dictionary<TSqlTokenType, IsSpaceRequired>
            {
                { TSqlTokenType.Plus, SpaceIsAlwaysRequired },
                { TSqlTokenType.Minus, SpaceIsAlwaysRequired },
                { TSqlTokenType.Star, null }, // SELECT COUNT(*)
                { TSqlTokenType.Divide, SpaceIsAlwaysRequired },
                { TSqlTokenType.MultiplyEquals, SpaceIsAlwaysRequired },
                { TSqlTokenType.EqualsSign, null },
                { TSqlTokenType.AddEquals, SpaceIsAlwaysRequired },
                { TSqlTokenType.SubtractEquals, SpaceIsAlwaysRequired },
                { TSqlTokenType.DivideEquals, SpaceIsAlwaysRequired },
                { TSqlTokenType.ModEquals, SpaceIsAlwaysRequired },
                { TSqlTokenType.BitwiseAndEquals, SpaceIsAlwaysRequired },
                { TSqlTokenType.BitwiseOrEquals, SpaceIsAlwaysRequired },
                { TSqlTokenType.BitwiseXorEquals, SpaceIsAlwaysRequired },
                { TSqlTokenType.ConcatEquals, SpaceIsAlwaysRequired },
                { TSqlTokenType.PercentSign, SpaceIsAlwaysRequired },
                { TSqlTokenType.LessThan, SpaceIsAlwaysRequired },
                { TSqlTokenType.GreaterThan, SpaceIsAlwaysRequired },
                { TSqlTokenType.Tilde, SpaceIsAlwaysRequired },
                { TSqlTokenType.LeftShift, SpaceIsAlwaysRequired },
                { TSqlTokenType.RightShift, SpaceIsAlwaysRequired }
            }
            .ToFrozenDictionary(a => a.Key, a => a.Value);

        public static readonly FrozenDictionary<TSqlTokenType, IsSpaceRequired> TokenTypesWhichRequireSpaceAfter = new Dictionary<TSqlTokenType, IsSpaceRequired>
            {
                { TSqlTokenType.Plus, null },
                { TSqlTokenType.Minus, null },
                { TSqlTokenType.Star, null },
                { TSqlTokenType.Divide, null },
                { TSqlTokenType.MultiplyEquals, null },
                { TSqlTokenType.EqualsSign, null },
                { TSqlTokenType.AddEquals, null },
                { TSqlTokenType.SubtractEquals, null },
                { TSqlTokenType.DivideEquals, null },
                { TSqlTokenType.ModEquals, null },
                { TSqlTokenType.BitwiseAndEquals, null },
                { TSqlTokenType.BitwiseOrEquals, null },
                { TSqlTokenType.BitwiseXorEquals, null },
                { TSqlTokenType.ConcatEquals, null },
                { TSqlTokenType.PercentSign, null },
                { TSqlTokenType.LessThan, null },
                { TSqlTokenType.GreaterThan, null },
                { TSqlTokenType.Tilde, null },
                { TSqlTokenType.LeftShift, null },
                { TSqlTokenType.RightShift, null },
                { TSqlTokenType.Comma, null }
            }
            .ToFrozenDictionary(a => a.Key, a => a.Value);
    }

/*
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


*/
}
