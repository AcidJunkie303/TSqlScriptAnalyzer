using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Security;

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
                return firstParameter?.ParameterValue is VariableReference;
            }

            return statement.ExecuteSpecification.ExecutableEntity switch
            {
                ExecutableProcedureReference => false,
                ExecutableStringList executableStringList when executableStringList.Strings.IsNullOrEmpty() => false,
                ExecutableStringList executableStringList when !executableStringList.Strings.IsNullOrEmpty() => !executableStringList.Strings.All(s => s is StringLiteral),
                _ => false
            };
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5000",
            IssueType.Warning,
            "Dynamic SQL",
            "Executing dynamic or external provided SQL can be dangerous and should be avoided."
        );
    }
}
