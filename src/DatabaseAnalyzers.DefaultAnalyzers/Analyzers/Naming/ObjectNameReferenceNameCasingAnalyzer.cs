using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

// TODO: remove
#pragma warning disable
public sealed class ObjectNameReferenceNameCasingAnalyzer : IScriptAnalyzer
{
    private readonly IAstService _astService;
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public ObjectNameReferenceNameCasingAnalyzer(IScriptAnalysisContext context, IAstService astService)
    {
        _context = context;
        _astService = astService;
        _script = context.Script;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript()
    {
        Analyze<NamedTableReference>(AnalyzeNamedTableReference);
//        Analyze<SchemaObjectFunctionTableReference>(AnalyzeSchemaObjectFunctionTableReference);
//        Analyze<FunctionCall>(AnalyzeFunctionCall);
//        Analyze<ColumnReferenceExpression>(AnalyzeColumnReferenceExpression);
    }

    private void Analyze<T>(Action<T> analyzerDelegate)
        where T : TSqlFragment
    {
        foreach (var fragment in _script.ParsedScript.GetChildren<T>(recursive: true))
        {
            analyzerDelegate(fragment);
        }
    }

    private void AnalyzeNamedTableReference(NamedTableReference tableReference)
    {
        var tableResolver = TableResolverOld.Create(_context, _astService, tableReference);
        var tableOrView = tableResolver.Resolve();
        if (tableOrView is null)
        {
        }
    }

    private void Report(TSqlFragment fragment, string objectTypeName, string usedName, string originalName)
    {
        var currentDatabaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragment);
        var fullObjectName = fragment.TryGetFirstClassObjectName(_context.DefaultSchemaName, _script.ParsedScript, _script.ParentFragmentProvider);
        _context.IssueReporter.Report(WellKnownDiagnosticDefinitions.MissingAlias, currentDatabaseName ?? DatabaseNames.Unknown, _script.RelativeScriptFilePath, fullObjectName, fragment.GetCodeRegion(),
            objectTypeName, usedName, originalName);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5061",
            IssueType.Formatting,
            "Object Name Reference with different casing",
            "The `{0}` reference `{1}` uses different casing than the original name `{2}`.",
            ["Object name", "The name used", "The original name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
