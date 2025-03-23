using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class AliasReferenceWithDifferentCasingAnalyzer : IScriptAnalyzer
{
    private readonly IColumnResolver _columnResolver;
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public AliasReferenceWithDifferentCasingAnalyzer(IScriptAnalysisContext context, IColumnResolverFactory columnResolverFactory)
    {
        _context = context;
        _columnResolver = columnResolverFactory.CreateColumnResolver(context);
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        foreach (var columnReference in _script.ParsedScript.GetChildren<ColumnReferenceExpression>(recursive: true))
        {
            AnalyzeColumnReference(columnReference);
        }
    }

    private void AnalyzeColumnReference(ColumnReferenceExpression columnReference)
    {
        if (columnReference.MultiPartIdentifier.Identifiers is null || columnReference.MultiPartIdentifier.Identifiers.Count != 2)
        {
            return;
        }

        var column = _columnResolver.Resolve(columnReference);
        if (column?.SourceAliasName is null)
        {
            return;
        }

        var aliasIdentifier = columnReference.MultiPartIdentifier.Identifiers[0];
        var aliasName = aliasIdentifier.Value;
        if (!aliasName.EqualsOrdinalIgnoreCase(column.SourceAliasName))
        {
            return;
        }

        if (aliasName.EqualsOrdinal(column.SourceAliasName))
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(columnReference) ?? _script.DatabaseName;
        var fullObjectName = columnReference.TryGetFirstClassObjectName(_context, _script);

        _context.IssueReporter.Report(
            DiagnosticDefinitions.Default,
            databaseName,
            _script.RelativeScriptFilePath,
            fullObjectName,
            aliasIdentifier.GetCodeRegion(),
            aliasName, column.SourceAliasName);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5065",
            IssueType.Warning,
            "Alias reference with different casing",
            "The alias `{0}` uses different casing to it's declaration `{1}`.",
            ["Declared alias name", "Alias reference name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
