using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class IntoSingleLineSqueezingAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public IntoSingleLineSqueezingAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        const string parameters = "parameters";
        const string columns = "columns";

        foreach (var fragment in _script.ParsedScript.GetChildren(recursive: true))
        {
            switch (fragment)
            {
                case ProcedureStatementBody procedureBody:
                    Analyze(procedureBody.Parameters, parameters);
                    break;

                case SelectStatement selectStatement:
                    Analyze(selectStatement);
                    break;

                case UpdateStatement updateStatement:
                    Analyze(updateStatement.UpdateSpecification.SetClauses, columns);
                    break;

                default:
                    continue;
            }
        }
    }

    private void Analyze(SelectStatement selectStatement)
    {
        const string objectTypeName = "columns";

        if (selectStatement.QueryExpression is not QuerySpecification querySpecification)
        {
            return;
        }

        Analyze(querySpecification.SelectElements, objectTypeName);
    }

    private void Analyze<T>(IList<T> fragments, string objectTypeName)
        where T : TSqlFragment
    {
        if (fragments.Count == 0)
        {
            return;
        }

        if (!ContainsMultipleItemsOnTheSameLine(fragments))
        {
            return;
        }

        Report(fragments, objectTypeName);
    }

    private void Report<T>(IList<T> fragments, string objectTypeName)
        where T : TSqlFragment
    {
        var codeRegion = fragments.CreateCodeRegionSpan();
        var fullObjectName = fragments[0].TryGetFirstClassObjectName(_context, _script);
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragments[0]) ?? DatabaseNames.Unknown;
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, codeRegion,
            objectTypeName);
    }

    private static bool ContainsMultipleItemsOnTheSameLine<T>(IList<T> fragments)
        where T : TSqlFragment
        => fragments
#if NET_9
            .CountBy(static a => a.StartLine)
            .Any(static a => a.Value > 1);
#else
            .GroupBy(static a => a.StartLine)
            .Any(static a => a.Count() > 1);
#endif

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5064",
            IssueType.Formatting,
            "Into single line squeezing",
            "Not all `{0}` are on a separate line.",
            ["Object type name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
