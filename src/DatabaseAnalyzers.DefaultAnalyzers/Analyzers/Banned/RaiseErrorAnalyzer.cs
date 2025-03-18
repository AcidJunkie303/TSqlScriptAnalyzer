using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Banned;

public sealed class RaiseErrorAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public RaiseErrorAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        IEnumerable<TSqlStatement> statements =
        [
            .. _script.ParsedScript.GetChildren<RaiseErrorStatement>(recursive: true),
            .. _script.ParsedScript.GetChildren<RaiseErrorLegacyStatement>(recursive: true)
        ];

        foreach (var statement in statements)
        {
            Analyze(statement);
        }
    }

    private void Analyze(TSqlStatement statement)
    {
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
        var fullObjectName = statement.TryGetFirstClassObjectName(_context, _script);
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, statement.GetCodeRegion());
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
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
