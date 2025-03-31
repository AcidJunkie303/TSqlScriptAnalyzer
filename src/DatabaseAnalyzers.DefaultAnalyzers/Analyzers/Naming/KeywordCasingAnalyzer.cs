using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Services;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class KeywordCasingAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;
    private readonly Aj5056Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public KeywordCasingAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter, Aj5056Settings settings)
    {
        _context = context;
        _issueReporter = issueReporter;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        if (_settings.KeywordNamingPolicy == Aj5056SKeywordNamingPolicy.Disabled)
        {
            return;
        }

        foreach (var token in _script.ParsedScript.ScriptTokenStream)
        {
            AnalyzeToken(token);
        }
    }

    private void AnalyzeToken(TSqlParserToken token) => AnalyzeKeyword(token);

    private void AnalyzeKeyword(TSqlParserToken token)
    {
        var shouldBeWrittenAs = KeywordCasingProvider.GetTokenCasing(token.TokenType, _settings.KeywordNamingPolicy);
        if (shouldBeWrittenAs is null)
        {
            return;
        }

        if (token.Text.IsNullOrWhiteSpace())
        {
            return;
        }

        if (shouldBeWrittenAs.EqualsOrdinal(token.Text))
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtToken(token) ?? DatabaseNames.Unknown;
        var fullObjectName = _script.ParsedScript
            .TryGetSqlFragmentAtPosition(token)
            ?.TryGetFirstClassObjectName(_context, _script);

        _issueReporter.Report(DiagnosticDefinitions.Default,
            databaseName,
            _script.RelativeScriptFilePath,
            fullObjectName,
            token.GetCodeRegion(),
            token.Text, shouldBeWrittenAs, _settings.KeywordNamingPolicy.ToString());
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5056",
            IssueType.Formatting,
            "Keyword uses wrong casing",
            "The keyword `{0}` should be written as `{1}` because of the configured keyword casing policy `{2}`.",
            ["The actual used keyword", "The expected keyword casing", "The configure keyword casing policy"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
