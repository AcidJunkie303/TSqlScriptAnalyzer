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
            var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragment) ?? DatabaseNames.Unknown;
            Report(context.IssueReporter, databaseName, script.RelativeScriptFilePath, fullObjectName, fragment);
        }
    }

    private static void Report(IIssueReporter issueReporter, string databaseName, string relativeScriptFilePath, string? fullObjectName, TSqlFragment fragment)
        => issueReporter.Report(DiagnosticDefinitions.Default, databaseName, relativeScriptFilePath, fullObjectName, fragment.GetCodeRegion());

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
