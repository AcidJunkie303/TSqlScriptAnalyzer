using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Tables;

public class MissingCommaBeforeConstraintKeywordAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, ScriptModel script)
    {
        foreach (var createTableStatement in script.Script.GetDescendantsOfType<SqlCreateTableStatement>())
        {
            AnalyzeTable(context, script.RelativeScriptFilePath, createTableStatement);
        }
    }

    private static void AnalyzeTable(IAnalysisContext context, string relativeScriptFilePath, SqlCreateTableStatement createTableStatement)
    {
        var fullObjectName = createTableStatement.TryGetFullObjectName(context.DefaultSchemaName);
        foreach (var column in createTableStatement.Definition.ColumnDefinitions)
        {
            AnalyzeColumn(context.IssueReporter, column, relativeScriptFilePath, fullObjectName);
        }
    }

    private static void AnalyzeColumn(IIssueReporter issueReporter, SqlColumnDefinition columnDefinition, string relativeScriptFilePath, string? fullObjectName)
    {
        var tokens = columnDefinition.Tokens
            .ToList();

        var (constraintToken, constraintTokenIndex) = tokens.FirstOrDefaultWithIndex(token => token.IsConstraint());
        if (constraintToken is null)
        {
            return;
        }

        for (var i = constraintTokenIndex - 1; i >= 0; i--)
        {
            var token = tokens[i];
            if (token.IsComment() || token.IsWhiteSpace())
            {
                continue;
            }

            if (token.IsComma())
            {
                return;
            }

            issueReporter.Report(DiagnosticDefinitions.Default, relativeScriptFilePath, fullObjectName, constraintToken);
            return;
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5005",
            IssueType.Warning,
            "Malformatted Create Table statement",
            "Missing comma before 'CONSTRAINT' keyword"
        );
    }
}
