using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

// TODO: remove
#pragma warning disable
public sealed class MissingIndexAnalyzer : IGlobalAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void Analyze(IAnalysisContext context)
    {
    }

    private static IEnumerable<List<TSqlParserToken>> GetConsecutiveBlankSpaceTokens(IList<TSqlParserToken> tokens)
    {
        List<TSqlParserToken>? currentGroup = null;

        foreach (var token in tokens)
        {
            if (token.TokenType == TSqlTokenType.WhiteSpace)
            {
                currentGroup ??= [];
                currentGroup.Add(token);
            }
            else if (currentGroup is not null)
            {
                yield return currentGroup;
                currentGroup = null;
            }
        }

        if (currentGroup is not null)
        {
            yield return currentGroup;
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5015",
            IssueType.Formatting,
            "Missing Index",
            "Multiple empty lines."
        );
    }

/*
    required information about a missing index:
        - diagnostic id
        - issue type = MissingIndex
        - database name (of table)
        - schema name (of table)
        - table name
        - column name (in table)
        - table creation script path (relative)
        - Used By object (where the filtering was done (WHERE, join condition)
            - object database name
            - object schema name
            - object name
            - object type ( stored procedure, function, script)
            - relative file path of object where the filtering was done
            - code region (where the filtering was done)
    ultimately, we will group the missing index entities by db, schema, table and column so the report shows all locations where the filtering is done

    IIssue properties:
    - IDiagnosticDefinition DiagnosticDefinition { get; }
    - string RelativeScriptFilePath { get; }
    - string? ObjectName { get; }
    - CodeRegion CodeRegion { get; }
    - IReadOnlyList<string> MessageInsertionStrings { get; }
    - string Message { get; }
    - public string FullObjectNameOrFileName => ObjectName ?? Path.GetFileName(RelativeScriptFilePath) ?? "Unknown";
 */
}
