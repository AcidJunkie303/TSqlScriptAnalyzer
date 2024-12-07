using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.DynamicSql;

public sealed class DynamicSqlAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
        => context.ReportIssue(DiagnosticDefinitions.Default, "script.sql", null, SourceSpan.Create(1, 1, 2, 2));

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5000",
            IssueType.Warning,
            "Dynamic SQL",
            "Dynamic SQL is not recommended.",
            0
        );
    }
}
