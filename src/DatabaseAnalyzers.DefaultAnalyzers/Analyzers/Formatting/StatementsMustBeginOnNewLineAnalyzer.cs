using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class StatementsMustBeginOnNewLineAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;
    private readonly Aj5023Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public StatementsMustBeginOnNewLineAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter, Aj5023Settings settings)
    {
        _context = context;
        _issueReporter = issueReporter;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        foreach (var statement in _script.ParsedScript.GetChildren<TSqlStatement>(recursive: true))
        {
            Analyze(statement);
        }
    }

    private void Analyze(TSqlStatement statement)
    {
        if (statement.FirstTokenIndex == 0)
        {
            return; // nothing to check here since this is the first token
        }

        var statementToken = statement.ScriptTokenStream[statement.FirstTokenIndex];
        if (_settings.StatementTypesToIgnore.Contains(statementToken.TokenType))
        {
            return;
        }

        for (var i = statement.FirstTokenIndex - 1; i >= 0; i--)
        {
            var token = statement.ScriptTokenStream[i];

            if (token.TokenType == TSqlTokenType.Semicolon)
            {
                continue;
            }

            if (token.TokenType == TSqlTokenType.WhiteSpace)
            {
                if (token.Text.Contains('\n', StringComparison.Ordinal))
                {
                    return;
                }

                continue;
            }

            var fullObjectName = statement.TryGetFirstClassObjectName(_context, _script);
            var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
            _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, statement.GetCodeRegion());

            return;
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5023",
            IssueType.Formatting,
            "Statements should begin on a new line",
            "Statements should begin on a new line.",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
