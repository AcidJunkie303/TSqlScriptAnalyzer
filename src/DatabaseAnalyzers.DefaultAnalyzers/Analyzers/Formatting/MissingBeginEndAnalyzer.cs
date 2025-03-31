using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class MissingBeginEndAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;
    private readonly Aj5022Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public MissingBeginEndAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter, Aj5022Settings settings)
    {
        _context = context;
        _issueReporter = issueReporter;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        if (_settings.WhileRequiresBeginEndBlock)
        {
            foreach (var statement in _script.ParsedScript.GetChildren<WhileStatement>(recursive: true))
            {
                AnalyzeWhileStatement(statement);
            }
        }

        if (_settings.IfRequiresBeginEndBlock)
        {
            foreach (var statement in _script.ParsedScript.GetChildren<IfStatement>(recursive: true))
            {
                AnalyzeIfStatement(statement.ThenStatement, "IF");
                if (statement.ElseStatement is not null)
                {
                    AnalyzeIfStatement(statement.ElseStatement, "ELSE");
                }
            }
        }
    }

    private void AnalyzeWhileStatement(WhileStatement statement)
    {
        if (statement.Statement is BeginEndBlockStatement)
        {
            return;
        }

        Report(statement.Statement, "WHILE");
    }

    private void AnalyzeIfStatement(TSqlStatement statement, string statementName)
    {
        if (statement is BeginEndBlockStatement)
        {
            return;
        }

        Report(statement, statementName);
    }

    private void Report(TSqlFragment fragmentToReport, string statementName)
    {
        var fullObjectName = fragmentToReport.TryGetFirstClassObjectName(_context, _script);
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragmentToReport) ?? DatabaseNames.Unknown;
        _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, fragmentToReport.GetCodeRegion(), statementName);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5022",
            IssueType.Formatting,
            "Missing BEGIN/END blocks",
            "The children of `{0}` should be enclosed in BEGIN/END blocks.",
            ["Statement name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
