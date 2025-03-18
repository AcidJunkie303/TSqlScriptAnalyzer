using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed class UnusedLabelAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public UnusedLabelAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript()
    {
        foreach (var batch in _script.ParsedScript.Batches)
        {
            AnalyzeBatch(batch);
        }
    }

    private void AnalyzeBatch(TSqlBatch batch)
    {
        var referencedLabelNames = batch
            .GetChildren<GoToStatement>(recursive: true)
            .Select(static a => a.LabelName.Value.TrimEnd(':'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var label in batch.GetChildren<LabelStatement>(recursive: true))
        {
            var labelName = label.Value.TrimEnd(':');
            if (referencedLabelNames.Contains(labelName))
            {
                continue;
            }

            var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(label) ?? DatabaseNames.Unknown;
            var fullObjectName = label.TryGetFirstClassObjectName(_context, _script);
            _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, label.GetCodeRegion(), labelName);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5036",
            IssueType.Warning,
            "Unreferenced Label",
            "The label `{0}` is not referenced and can be removed.",
            ["Label name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
