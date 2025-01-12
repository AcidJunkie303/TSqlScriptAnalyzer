using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Banned;

public sealed class RaiseErrorAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        IEnumerable<TSqlStatement> statements =
        [
            .. script.ParsedScript.GetChildren<RaiseErrorStatement>(recursive: true),
            .. script.ParsedScript.GetChildren<RaiseErrorLegacyStatement>(recursive: true)
        ];

        foreach (var statement in statements)
        {
            Analyze(context, script, statement);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, TSqlStatement statement)
    {
        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
        var fullObjectName = statement.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, statement.GetCodeRegion());
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5042",
            IssueType.Warning,
            "Usage of RAISERROR",
            "`RAISERROR` should not be used anymore. Use `THROW` instead.",
            [],
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
