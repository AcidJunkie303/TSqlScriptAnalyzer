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

        for (var i = 1; i < tokens.Count - 1; i++)
        {
            var token = tokens[i];

            if (Sets.PrecedingBlankSpaceEvaluatorsByTokenType.TryGetValue(token.TokenType, out var precedingEvaluator))
            {
                if (!precedingEvaluator(tokens, i))
                {
                    Report(token, "before");
                }
            }

            if (Sets.SucceedingBlankSpaceEvaluatorsByTokenType.TryGetValue(token.TokenType, out var succeedingEvaluator))
            {
                if (!succeedingEvaluator(tokens, i))
                {
                    Report(token, "after");
                }
            }
        }
    }

    private void Report(TSqlParserToken token, string beforeOrAfter)
    {
        var fullObjectName = _script.ParsedScript
            .TryGetSqlFragmentAtPosition(token)
            ?.TryGetFirstClassObjectName(_context, _script);

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtToken(token) ?? DatabaseNames.Unknown;
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, token.GetCodeRegion(), beforeOrAfter, token.Text);
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
        public static readonly FrozenDictionary<TSqlTokenType, IsSpaceRequired> PrecedingBlankSpaceEvaluatorsByTokenType = new Dictionary<TSqlTokenType, IsSpaceRequired>
            {
                { TSqlTokenType.Plus, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.Minus, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.Star, BlankSpaceComplianceEvaluators.Before.Star },
                { TSqlTokenType.Divide, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.MultiplyEquals, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.EqualsSign, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.AddEquals, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.SubtractEquals, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.DivideEquals, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.ModEquals, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.BitwiseAndEquals, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.BitwiseOrEquals, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.BitwiseXorEquals, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.ConcatEquals, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.PercentSign, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.LessThan, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.GreaterThan, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.Tilde, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.LeftShift, BlankSpaceComplianceEvaluators.Before.General },
                { TSqlTokenType.RightShift, BlankSpaceComplianceEvaluators.Before.General }
            }
            .ToFrozenDictionary(a => a.Key, a => a.Value);

        public static readonly FrozenDictionary<TSqlTokenType, IsSpaceRequired> SucceedingBlankSpaceEvaluatorsByTokenType = new Dictionary<TSqlTokenType, IsSpaceRequired>
            {
                { TSqlTokenType.Plus, BlankSpaceComplianceEvaluators.After.PlusOrMinus },
                { TSqlTokenType.Minus, BlankSpaceComplianceEvaluators.After.PlusOrMinus },
                { TSqlTokenType.Star, BlankSpaceComplianceEvaluators.After.Star },
                { TSqlTokenType.Divide, BlankSpaceComplianceEvaluators.After.General },
                { TSqlTokenType.MultiplyEquals, BlankSpaceComplianceEvaluators.After.General },
                { TSqlTokenType.EqualsSign, BlankSpaceComplianceEvaluators.After.EqualSign },
                { TSqlTokenType.AddEquals, BlankSpaceComplianceEvaluators.After.General },
                { TSqlTokenType.SubtractEquals, BlankSpaceComplianceEvaluators.After.General },
                { TSqlTokenType.DivideEquals, BlankSpaceComplianceEvaluators.After.General },
                { TSqlTokenType.ModEquals, BlankSpaceComplianceEvaluators.After.General },
                { TSqlTokenType.BitwiseAndEquals, BlankSpaceComplianceEvaluators.After.General },
                { TSqlTokenType.BitwiseOrEquals, BlankSpaceComplianceEvaluators.After.General },
                { TSqlTokenType.BitwiseXorEquals, BlankSpaceComplianceEvaluators.After.General },
                { TSqlTokenType.ConcatEquals, BlankSpaceComplianceEvaluators.After.General },
                { TSqlTokenType.PercentSign, BlankSpaceComplianceEvaluators.After.General },
                { TSqlTokenType.LessThan, BlankSpaceComplianceEvaluators.After.General },
                { TSqlTokenType.GreaterThan, BlankSpaceComplianceEvaluators.After.General },
                { TSqlTokenType.Tilde, BlankSpaceComplianceEvaluators.After.General },
                { TSqlTokenType.LeftShift, BlankSpaceComplianceEvaluators.After.General },
                { TSqlTokenType.RightShift, BlankSpaceComplianceEvaluators.After.General },
                { TSqlTokenType.Comma, BlankSpaceComplianceEvaluators.After.General }
            }
            .ToFrozenDictionary(a => a.Key, a => a.Value);
    }
}
