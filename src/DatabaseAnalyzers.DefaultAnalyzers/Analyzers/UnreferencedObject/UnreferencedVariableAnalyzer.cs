using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UnreferencedObject;

public sealed class UnreferencedVariableAnalyzer : IScriptAnalyzer
{
    private readonly IAnalysisContext _context;
    private readonly IScriptModel _script;

    public UnreferencedVariableAnalyzer(IScriptAnalysisContext context)
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
        var referencedVariableNames = batch
            .GetChildren<VariableReference>(recursive: true)
            .Select(static a => a.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var variableDeclarations = batch
            .GetChildren<DeclareVariableElement>(recursive: true)
            .Where(static a => a is not ProcedureParameter);

        foreach (var variableDeclaration in variableDeclarations)
        {
            if (referencedVariableNames.Contains(variableDeclaration.VariableName.Value))
            {
                continue;
            }

            var fullObjectName = variableDeclaration.TryGetFirstClassObjectName(_context, _script);
            var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(variableDeclaration) ?? DatabaseNames.Unknown;
            _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, variableDeclaration.GetCodeRegion(), variableDeclaration.VariableName.Value);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5012",
            IssueType.Warning,
            "Unreferenced variable",
            "The variable `{0}` is declared but not used.",
            ["Variable name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
