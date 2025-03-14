using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class MissingTableAliasAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var columnReference in script.ParsedScript.GetChildren<ColumnReferenceExpression>(recursive: true))
        {
            Analyze(context, script, columnReference);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, ColumnReferenceExpression columnReference)
    {
        var querySpecification = columnReference
            .GetParents(script.ParentFragmentProvider)
            .OfType<QuerySpecification>()
            .FirstOrDefault();

        if (querySpecification?.FromClause is null || columnReference.MultiPartIdentifier is null)
        {
            return;
        }

        if (columnReference.MultiPartIdentifier.Count > 1)
        {
            return;
        }

        if (IsFirstArgumentOfDateAddOrDatePart(script, columnReference))
        {
            return;
        }

        var tableReferences = querySpecification.FromClause.TableReferences;
        var hasMultipleTableReferencesOrJoins = tableReferences.Count > 1 || tableReferences.OfType<JoinTableReference>().Any();
        if (!hasMultipleTableReferencesOrJoins)
        {
            return;
        }

        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(columnReference) ?? DatabaseNames.Unknown;
        var fullObjectName = columnReference.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, columnReference.GetCodeRegion(), columnReference.GetSql());
    }

    private static bool IsFirstArgumentOfDateAddOrDatePart(IScriptModel script, ColumnReferenceExpression columnReference)
    {
        if (columnReference.GetParent(script.ParentFragmentProvider) is not FunctionCall functionCall)
        {
            return false;
        }

        if (!functionCall.FunctionName.Value.EqualsOrdinalIgnoreCase("DateAdd") && !functionCall.FunctionName.Value.EqualsOrdinalIgnoreCase("DatePart"))
        {
            return false;
        }

        if (functionCall.Parameters.Count == 0)
        {
            return false;
        }

        var firstParameter = functionCall.Parameters[0];
        return firstParameter == columnReference;
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5016",
            IssueType.Warning,
            "Missing table alias when more than one table is involved in a statement",
            "Missing alias in expression `{0}`.",
            ["Expression"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
