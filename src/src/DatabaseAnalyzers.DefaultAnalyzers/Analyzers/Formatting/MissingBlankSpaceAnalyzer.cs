using System.Collections.Frozen;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class MissingBlankSpaceAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var tokens = script.ParsedScript.Tokens.ToList();

        // we skip the first and last since it doesn't make sense to check them, and it also makes the checking easier (out of bounds checking)
        for (var i = 1; i < tokens.Count - 1; i++)
        {
            var token = tokens[i];
            if (RequiresSpaceBefore(token))
            {
                var previousToken = tokens[i - 1];
                if (!previousToken.IsWhiteSpace())
                {
                    Report(token, "before");
                }
            }

            if (RequiresSpaceAfter(token))
            {
                var nextToken = tokens[i + 1];
                if (!nextToken.IsWhiteSpace())
                {
                    Report(token, "after");
                }
            }
        }

        void Report(Token token, string beforeOrAfter)
        {
            var fullObjectName = script.ParsedScript.TryGetFullObjectNameAtPosition(context.DefaultSchemaName, token.StartLocation);
            var codeRegion = CodeRegion.From(token);
            context.IssueReporter.Report(DiagnosticDefinitions.Default, script, fullObjectName, codeRegion, beforeOrAfter, token.Text);
        }
    }

    private static bool RequiresSpaceBefore(Token token) => Sets.TokenTypesWhichRequireSpaceBeforeAndAfter.Contains(token.Type);

    private static bool RequiresSpaceAfter(Token token)
    {
        return Sets.TokenTypesWhichRequireSpaceBeforeAndAfter.Contains(token.Type)
               || Sets.TokenTypesWhichRequireSpaceAfter.Contains(token.Type);
    }

    private static class Sets
    {
        public static readonly FrozenSet<string> TokenTypesWhichRequireSpaceBeforeAndAfter = new[]
            {
                "+",
                "-",
                "*",
                "/",
                "%",
                "+=",
                "-=",
                "*-",
                "/="
            }
            .ToFrozenSet(StringComparer.Ordinal);

        public static readonly FrozenSet<string> TokenTypesWhichRequireSpaceAfter = new[]
            {
                ","
            }
            .ToFrozenSet(StringComparer.Ordinal);
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
