using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class MissingEmptyLineAroundGoStatementAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;
    private readonly Aj5045Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public MissingEmptyLineAroundGoStatementAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter, Aj5045Settings settings)
    {
        _context = context;
        _issueReporter = issueReporter;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        if (_settings is { RequireEmptyLineBeforeGo: false, RequireEmptyLineAfterGo: false })
        {
            return;
        }

        for (var i = 0; i < _script.ParsedScript.ScriptTokenStream.Count; i++)
        {
            var token = _script.ParsedScript.ScriptTokenStream[i];
            if (token.TokenType != TSqlTokenType.Go)
            {
                continue;
            }

            AnalyzeToken(token, i);
        }
    }

    private void AnalyzeToken(TSqlParserToken goStatementToken, int tokenIndex)
    {
        var missingBefore = _settings.RequireEmptyLineBeforeGo && IsMissingEmptyLineBefore();
        var missingAfter = _settings.RequireEmptyLineAfterGo && IsMissingEmptyLineAfter();
        if (!missingBefore && !missingAfter)
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtLocation(goStatementToken.Line, goStatementToken.Column) ?? DatabaseNames.Unknown;
        var codeRegion = goStatementToken.GetCodeRegion();
        var fullObjectName = _script.ParsedScript
            .TryGetSqlFragmentAtPosition(goStatementToken)
            ?.TryGetFirstClassObjectName(_context, _script);

        if (missingBefore)
        {
            _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, codeRegion, "before");
        }

        if (missingAfter)
        {
            _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, codeRegion, "after");
        }

        bool IsMissingEmptyLineAfter()
        {
            if (tokenIndex == 0 || !_settings.RequireEmptyLineAfterGo)
            {
                return false;
            }

            return GetNewLineCountAfterToken(tokenIndex) < 2;
        }

        bool IsMissingEmptyLineBefore()
        {
            if (tokenIndex == 0 || !_settings.RequireEmptyLineBeforeGo)
            {
                return false;
            }

            return GetNewLineCountBeforeToken(tokenIndex) < 2;
        }
    }

    private int GetNewLineCountAfterToken(int tokenIndex)
        => _script.ParsedScript.ScriptTokenStream
            .Skip(tokenIndex + 1)
            .TakeWhile(t => t.TokenType is TSqlTokenType.WhiteSpace or TSqlTokenType.EndOfFile)
            .Sum(a =>
                (a.Text?.Count(c => c == '\n') ?? 0)
                + (a.TokenType == TSqlTokenType.EndOfFile ? 1 : 0)
            );

    private int GetNewLineCountBeforeToken(int tokenIndex)
        => _script.ParsedScript.ScriptTokenStream
            .Take(tokenIndex)
            .Reverse()
            .TakeWhile(t => t.TokenType == TSqlTokenType.WhiteSpace)
            .Sum(a => a.Text.Count(c => c == '\n'));

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5045",
            IssueType.Formatting,
            "Missing empty line before/after GO batch separators",
            "Missing empty line `{0}` GO statement.",
            ["before/after"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
