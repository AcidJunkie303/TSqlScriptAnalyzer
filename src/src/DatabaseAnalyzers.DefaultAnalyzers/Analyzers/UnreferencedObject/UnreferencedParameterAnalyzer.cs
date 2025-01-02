using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UnreferencedObject;

public sealed class UnreferencedParameterAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var procedures = script.ParsedScript.GetTopLevelDescendantsOfType<ProcedureStatementBody>();
        var functions = script.ParsedScript.GetTopLevelDescendantsOfType<FunctionStatementBody>();

        Analyze(context, script, procedures, static a => a.Parameters, static a => a.StatementList);
        Analyze(context, script, functions, static a => a.Parameters, static a => a.StatementList);
    }

    private static void Analyze<T>(IAnalysisContext context, IScriptModel script, IEnumerable<T> creationStatements, Func<T, IList<ProcedureParameter>> parametersProvider, Func<T, StatementList?> statementListProvider)
        where T : TSqlFragment
    {
        foreach (var creationStatement in creationStatements)
        {
            Analyze(context, script, creationStatement, parametersProvider, statementListProvider);
        }
    }

    private static void Analyze<T>(IAnalysisContext context, IScriptModel script, T creationStatement, Func<T, IList<ProcedureParameter>> parametersProvider, Func<T, StatementList?> statementListProvider)
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

            var fullObjectName = parameter.TryGetFirstClassObjectName(context, script);
            var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(parameter) ?? DatabaseNames.Unknown;
            context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, parameter.GetCodeRegion(), parameter.VariableName.Value);
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
            "The parameter '{0}' is not referenced"
        );
    }
}
