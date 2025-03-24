using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class MissingTableAliasAnalyzer : IScriptAnalyzer
{
    private readonly IAstService _astService;
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public MissingTableAliasAnalyzer(IScriptAnalysisContext context, IAstService astService)
    {
        _context = context;
        _astService = astService;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        foreach (var columnReference in _script.ParsedScript.GetChildren<ColumnReferenceExpression>(recursive: true))
        {
            Analyze(columnReference);
        }
    }

    private void Analyze(ColumnReferenceExpression columnReference)
    {
        var querySpecification = columnReference
            .GetParents(_script.ParentFragmentProvider)
            .OfType<QuerySpecification>()
            .FirstOrDefault();

        if (querySpecification?.FromClause is null || columnReference.MultiPartIdentifier is null)
        {
            return;
        }

        if (columnReference.MultiPartIdentifier.Count > 1)
        {
            return;
        }

        if (_astService.IsChildOfFunctionEnumParameter(columnReference, _script.ParentFragmentProvider))
        {
            return;
        }

        var tableReferences = querySpecification.FromClause.TableReferences;
        var hasMultipleTableReferencesOrJoins = tableReferences.Count > 1 || tableReferences.OfType<JoinTableReference>().Any();
        if (!hasMultipleTableReferencesOrJoins)
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(columnReference) ?? DatabaseNames.Unknown;
        var fullObjectName = columnReference.TryGetFirstClassObjectName(_context, _script);
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, columnReference.GetCodeRegion(), columnReference.GetSql());
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5016",
            IssueType.Warning,
            "Missing table alias when more than one table is involved in a statement",
            "Missing alias in expression `{0}`.",
            ["Expression"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
