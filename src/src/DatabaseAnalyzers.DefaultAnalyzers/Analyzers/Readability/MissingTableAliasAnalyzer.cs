using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Readability;

public sealed class MissingTableAliasAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

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

        if (querySpecification?.FromClause is null)
        {
            return;
        }

        if (columnReference.MultiPartIdentifier.Count > 1)
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

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5016",
            IssueType.Warning,
            "Missing table alias when more than two involved sources",
            "Missing alias in expression '{0}'."
        );
    }
}
