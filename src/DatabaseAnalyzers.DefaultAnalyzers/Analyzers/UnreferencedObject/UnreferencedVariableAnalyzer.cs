using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UnreferencedObject;

public sealed class UnreferencedVariableAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public UnreferencedVariableAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter)
    {
        _context = context;
        _issueReporter = issueReporter;
        _script = context.Script;
    }

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
            _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, variableDeclaration.GetCodeRegion(), variableDeclaration.VariableName.Value);
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
