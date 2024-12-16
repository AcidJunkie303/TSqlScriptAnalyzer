using System.Collections.Frozen;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class MissingBlankSpaceAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var tokens = script.ParsedScript.ScriptTokenStream.ToList();

        // we skip the first and last since it doesn't make sense to check them, and it also makes the checking easier (out of bounds checking)
        for (var i = 1; i < tokens.Count - 1; i++)
        {
            var token = tokens[i];
            if (RequiresSpaceBefore(token))
            {
                var previousToken = tokens[i - 1];
                if (previousToken.TokenType != TSqlTokenType.WhiteSpace)
                {
                    Report(token, "before");
                }
            }

            if (RequiresSpaceAfter(token))
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
            var fullObjectName = script.ParsedScript
                .TryGetSqlFragmentAtPosition(token)
                ?.TryGetFirstClassObjectName(context, script);

            context.IssueReporter.Report(DiagnosticDefinitions.Default, script, fullObjectName, token, beforeOrAfter, token.Text);
        }
    }

    private static bool RequiresSpaceBefore(TSqlParserToken token)
        => Sets.TokenTypesWhichRequireSpaceBeforeAndAfter.Contains(token.TokenType);

    private static bool RequiresSpaceAfter(TSqlParserToken token)
    {
        return Sets.TokenTypesWhichRequireSpaceBeforeAndAfter.Contains(token.TokenType)
               || Sets.TokenTypesWhichRequireSpaceAfter.Contains(token.TokenType);
    }

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
                TSqlTokenType.SingleLineComment,
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
            "Missing blank-space {0} '{1}'"
        );
    }
}
