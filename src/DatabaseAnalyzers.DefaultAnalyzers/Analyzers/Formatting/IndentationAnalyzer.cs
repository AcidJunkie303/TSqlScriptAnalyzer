using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class IndentationAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public IndentationAnalyzer(IScriptAnalysisContext context)
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

        if (HasMultipleItemsOnTheSameLine(fragments))
        {
            // Handled by IntoSingleLineSqueezingAnalyzer
            return;
        }

        if (AllHaveSameIndentation(fragments))
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

    private static bool HasMultipleItemsOnTheSameLine<T>(IList<T> fragments)
        where T : TSqlFragment
        => fragments
            .Select(a => a.StartLine)
            .GroupBy(a => a)
            .Any(a => a.Count() > 1);

    private static bool AllHaveSameIndentation<T>(IList<T> fragments)
        where T : TSqlFragment
    {
        if (fragments.Count == 0)
        {
            return true;
        }

        return fragments
            .Select(a => a.GetCodeLocation().Column)
            .DistinctCount() == 1;
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5063",
            IssueType.Formatting,
            "Uneven Indentation",
            "The `{0}` do not share the same indentation level.",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
