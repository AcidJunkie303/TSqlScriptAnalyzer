using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.DynamicSql;

public sealed class DynamicSqlAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, ScriptModel script)
    {
        var settings = context.DiagnosticSettingsRetriever.GetSettings<Aj5000Settings>("AJ5000");
        Console.WriteLine(settings is null ? "No settings found" : $"Settings found: {settings.ExampleValue}");

        context.IssueReporter.Report(DiagnosticDefinitions.Default, script.FullScriptFilePath, null, SourceSpan.Create(1, 1, 2, 2));
    }

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
