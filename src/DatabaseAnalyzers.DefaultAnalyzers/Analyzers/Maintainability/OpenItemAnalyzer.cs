using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed class OpenItemAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;
    private readonly Aj5004Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public OpenItemAnalyzer(IScriptAnalysisContext context, Aj5004Settings settings)
    {
        _context = context;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        if (IsScriptExcluded())
        {
            return;
        }

        var commentTokens = _script.ParsedScript.ScriptTokenStream
            .Where(static a => a.TokenType is TSqlTokenType.SingleLineComment or TSqlTokenType.MultilineComment);

        foreach (var commentToken in commentTokens)
        {
            AnalyzeToken(commentToken);
        }
    }

    private bool IsScriptExcluded() => _settings.ExcludedFilePathPatterns.Any(pattern => pattern.IsMatch(_script.RelativeScriptFilePath));

    private void AnalyzeToken(TSqlParserToken commentToken)
    {
        foreach (var (topic, expression) in _settings.TopicsAndPatterns)
        {
            var match = expression.Match(commentToken.Text);
            if (!match.Success)
            {
                continue;
            }

            var message = match.Groups.TryGetValue("message", out var group)
                ? group.Value
                : "Unable to parse message. Make sure that the regex pattern contain a named group capture called 'message'.";

            var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(_script.ParsedScript) ?? _script.DatabaseName;
            var fullObjectName = _script.ParsedScript
                .TryGetSqlFragmentAtPosition(commentToken)
                ?.TryGetFirstClassObjectName(_context, _script);

            _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, commentToken.GetCodeRegion(), topic, message);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5004",
            IssueType.Information,
            "Open Item",
            "Found `{0}`: {1}",
            ["Topic", "Message"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
