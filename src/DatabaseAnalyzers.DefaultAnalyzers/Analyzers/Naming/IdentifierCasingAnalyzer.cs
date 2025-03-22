using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class IdentifierCasingAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;
    private readonly Aj5057Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public IdentifierCasingAnalyzer(IScriptAnalysisContext context, Aj5057Settings settings)
    {
        _context = context;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        if (_settings.CasingByIdentifier.Count == 0)
        {
            return;
        }

        foreach (var token in _script.ParsedScript.ScriptTokenStream)
        {
            if (token.TokenType == TSqlTokenType.Identifier)
            {
                AnalyzeIdentifier(token);
                return;
            }
        }
    }

    private void AnalyzeIdentifier(TSqlParserToken token)
    {
        if (token.Text.IsNullOrWhiteSpace())
        {
            return;
        }

        var shouldBeWrittenAs = _settings.CasingByIdentifier.GetValueOrDefault(token.Text);
        if (shouldBeWrittenAs is null || token.Text.EqualsOrdinal(shouldBeWrittenAs))
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtToken(token) ?? DatabaseNames.Unknown;
        var fullObjectName = _script.ParsedScript
            .TryGetSqlFragmentAtPosition(token)
            ?.TryGetFirstClassObjectName(_context, _script);

        _context.IssueReporter.Report(DiagnosticDefinitions.Default,
            databaseName,
            _script.RelativeScriptFilePath,
            fullObjectName,
            token.GetCodeRegion(),
            token.Text, shouldBeWrittenAs);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5057",
            IssueType.Formatting,
            "Identifier uses wrong casing",
            "The identifier `{0}` should be written as `{1}`.",
            ["The actual used keyword", "The expected keyword casing"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
