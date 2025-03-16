using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class VariableReferenceWithDifferentCasingAnalyzer : IScriptAnalyzer
{
    private readonly IAnalysisContext _context;
    private readonly IScriptModel _script;

    public VariableReferenceWithDifferentCasingAnalyzer(IScriptAnalysisContext context)
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
        var variableDeclarationsByName = batch
            .GetChildren<DeclareVariableStatement>(recursive: true)
            .SelectMany(static a => a.Declarations)
            .GroupBy(static a => a.VariableName.Value, StringComparer.OrdinalIgnoreCase)
            .Select(static a => a.First())
            .ToDictionary(static a => a.VariableName.Value, static a => a, StringComparer.OrdinalIgnoreCase);

        foreach (var variableReference in batch.GetChildren<VariableReference>(recursive: true))
        {
            var variableReferenceName = variableReference.Name;

            // case-insensitive lookup
            if (!variableDeclarationsByName.TryGetValue(variableReferenceName, out var variableDeclaration))
            {
                continue;
            }

            if (variableReferenceName.EqualsOrdinal(variableDeclaration.VariableName.Value))
            {
                continue;
            }

            var fullObjectName = variableReference.TryGetFirstClassObjectName(_context, _script);
            var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(variableReference) ?? DatabaseNames.Unknown;
            _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, variableReference.GetCodeRegion(), variableReference.Name, variableDeclaration.VariableName.Value);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5014",
            IssueType.Warning,
            "Variable reference with different casing",
            "The variable reference `{0}` has different casing compared to the declaration `{1}`.",
            ["Variable name", "Declared variable name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
