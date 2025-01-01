using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingOrderByWhenSelectTopAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var statement in script.ParsedScript.GetChildren<SelectStatement>(recursive: true))
        {
            Analyze(context, script, statement);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, SelectStatement statement)
    {
        if (statement.QueryExpression is not QuerySpecification querySpecification)
        {
            return;
        }

        if (querySpecification.TopRowFilter is null)
        {
            return;
        }

        if (querySpecification.OrderByClause is not null)
        {
            return;
        }

        var databaseName = statement.FindCurrentDatabaseNameAtFragment(script.ParsedScript);
        var fullObjectName = statement.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, statement.GetCodeRegion());
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5043",
            IssueType.Warning,
            "Missing ORDER BY clause when using TOP",
            "Not using 'ORDER BY' in combination with 'TOP' might lead to non-deterministic results."
        );
    }
}
