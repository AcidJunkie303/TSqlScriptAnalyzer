using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;

public sealed class ObjectCreationWithoutOrAlterAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        AnalyzeFragments(context, script, script.ParsedScript.GetChildren<CreateViewStatement>(recursive: true));
        AnalyzeFragments(context, script, script.ParsedScript.GetChildren<CreateProcedureStatement>(recursive: true));
        AnalyzeFragments(context, script, script.ParsedScript.GetChildren<CreateFunctionStatement>(recursive: true));
        AnalyzeFragments(context, script, script.ParsedScript.GetChildren<CreateTriggerStatement>(recursive: true));
    }

    private static void AnalyzeFragments(IAnalysisContext context, IScriptModel script, IEnumerable<TSqlFragment> fragments)
    {
        foreach (var fragment in fragments)
        {
            var fullObjectName = fragment.TryGetFirstClassObjectName(context, script);
            Report(context.IssueReporter, script, fullObjectName, fragment);
        }
    }

    private static void Report(IIssueReporter issueReporter, IScriptModel script, string? fullObjectName, TSqlFragment fragment)
        => issueReporter.Report(DiagnosticDefinitions.Default, script, fullObjectName, fragment);

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5009",
            IssueType.Formatting,
            "Object creation without 'OR ALTER' clause",
            "Object creation without 'OR ALTER' clause."
        );
    }
}
