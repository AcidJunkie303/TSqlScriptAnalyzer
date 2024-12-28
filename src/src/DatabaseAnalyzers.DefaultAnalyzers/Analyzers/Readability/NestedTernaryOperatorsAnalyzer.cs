using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Readability;

public sealed class NestedTernaryOperatorsAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var expression in script.ParsedScript.GetChildren<IIfCall>(true))
        {
            Analyze(context, script, expression);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, IIfCall expression)
    {
        if (!expression.GetParents(script.ParentFragmentProvider).OfType<IIfCall>().Any())
        {
            return;
        }

        var databaseName = expression.FindCurrentDatabaseNameAtFragment(script.ParsedScript);
        var fullObjectName = expression.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, expression.GetCodeRegion());
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5033",
            IssueType.Warning,
            "Ternary operators should not be nested",
            "Ternary operators 'IIF' should not be nested."
        );
    }
}
