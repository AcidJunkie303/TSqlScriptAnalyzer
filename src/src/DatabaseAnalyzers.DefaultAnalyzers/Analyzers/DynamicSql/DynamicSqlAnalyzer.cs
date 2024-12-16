using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.DynamicSql;

public sealed class DynamicSqlAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var statement in script.ParsedScript.GetChildren<ExecuteStatement>(recursive: true))
        {
            Analyze(context, script, statement);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, ExecuteStatement statement)
    {
        if (!IsDynamicSql())
        {
            return;
        }

        var fullObjectName = statement.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, script, fullObjectName, statement);

        bool IsDynamicSql()
        {
            if (statement.ExecuteSpecification.ExecutableEntity is ExecutableProcedureReference procedureReference)
            {
                var firstParameter = procedureReference.Parameters.FirstOrDefault();
                return firstParameter?.ParameterValue is StringLiteral or VariableReference;
            }

            return statement.ExecuteSpecification.ExecutableEntity is ExecutableStringList;
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5000",
            IssueType.Warning,
            "Dynamic SQL",
            "Dynamic SQL is not recommended."
        );
    }
}
