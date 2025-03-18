using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UnreferencedObject;

public sealed class UnreferencedParameterAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public UnreferencedParameterAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript()
    {
        var procedures = _script.ParsedScript.GetTopLevelDescendantsOfType<ProcedureStatementBody>(_script.ParentFragmentProvider);
        var functions = _script.ParsedScript.GetTopLevelDescendantsOfType<FunctionStatementBody>(_script.ParentFragmentProvider);

        Analyze(procedures, static a => a.Parameters, static a => a.StatementList);
        Analyze(functions, static a => a.Parameters, static a => a.StatementList);
    }

    private void Analyze<T>(IEnumerable<T> creationStatements, Func<T, IList<ProcedureParameter>> parametersProvider, Func<T, StatementList?> statementListProvider)
        where T : TSqlFragment
    {
        foreach (var creationStatement in creationStatements)
        {
            Analyze(creationStatement, parametersProvider, statementListProvider);
        }
    }

    private void Analyze<T>(T creationStatement, Func<T, IList<ProcedureParameter>> parametersProvider, Func<T, StatementList?> statementListProvider)
        where T : TSqlFragment
    {
        var statementList = statementListProvider(creationStatement);
        if (statementList is null)
        {
            return;
        }

        var referencedVariableNames = GetReferencedVariableNames(statementList);

        foreach (var parameter in parametersProvider(creationStatement))
        {
            if (referencedVariableNames.Contains(parameter.VariableName.Value))
            {
                continue;
            }

            var fullObjectName = parameter.TryGetFirstClassObjectName(_context, _script);
            var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(parameter) ?? DatabaseNames.Unknown;
            _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, parameter.GetCodeRegion(), parameter.VariableName.Value);
        }
    }

    private static HashSet<string> GetReferencedVariableNames(StatementList statementList)
        => statementList
            .GetChildren<VariableReference>(recursive: true)
            .Select(static a => a.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5011",
            IssueType.Warning,
            "Unreferenced parameter",
            "The parameter `{0}` is not referenced.",
            ["Parameter name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
