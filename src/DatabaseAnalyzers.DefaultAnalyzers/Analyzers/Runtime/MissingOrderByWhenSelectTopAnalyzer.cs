using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingOrderByWhenSelectTopAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public MissingOrderByWhenSelectTopAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter)
    {
        _context = context;
        _issueReporter = issueReporter;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        foreach (var statement in _script.ParsedScript.GetChildren<SelectStatement>(recursive: true))
        {
            Analyze(statement);
        }
    }

    private void Analyze(SelectStatement statement)
    {
        if (statement.QueryExpression is not QuerySpecification querySpecification)
        {
            return;
        }

        if (querySpecification.TopRowFilter is null)
        {
            return;
        }

        if (querySpecification.OrderByClause is not null)
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
        var fullObjectName = statement.TryGetFirstClassObjectName(_context, _script);
        _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, statement.GetCodeRegion());
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5043",
            IssueType.Warning,
            "Missing ORDER BY clause when using TOP",
            "Not using `ORDER BY` in combination with `TOP` might lead to non-deterministic results.",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
