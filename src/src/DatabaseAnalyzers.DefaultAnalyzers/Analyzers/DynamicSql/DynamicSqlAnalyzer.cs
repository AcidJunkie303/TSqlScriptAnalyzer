using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.DynamicSql;

public sealed class DynamicSqlAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, ScriptModel script)
    {
        foreach (var statement in script.ParsedScript.GetDescendantsOfType<SqlExecuteModuleStatement>())
        {
            Analyze(context, script, statement);
        }

        foreach (var statement in script.ParsedScript.GetDescendantsOfType<SqlExecuteStringStatement>())
        {
            Analyze(context, script, statement);
        }
    }

    private static void Analyze(IAnalysisContext context, ScriptModel script, SqlExecuteModuleStatement statement)
    {
        var firstChild = statement.Children.FirstOrDefault();
        if (firstChild is SqlObjectReference objectReference && !objectReference.ObjectIdentifier.ObjectName.Value.EqualsOrdinalIgnoreCase("sp_executeSql"))
        {
            return;
        }

        var argument = statement.Arguments?.FirstOrDefault();
        if (argument is null)
        {
            return;
        }

        if (!argument.Children.Any(a => a is SqlScalarVariableRefExpression or SqlLiteralExpression))
        {
            return;
        }

        var fullObjectName = statement.TryGetFullObjectName(context.DefaultSchemaName);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, script.RelativeScriptFilePath, fullObjectName, statement);
    }

    private static void Analyze(IAnalysisContext context, ScriptModel script, SqlExecuteStringStatement statement)
    {
        var fullObjectName = statement.TryGetFullObjectName(context.DefaultSchemaName);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, script.RelativeScriptFilePath, fullObjectName, statement);
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
