using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using DatabaseAnalyzers.DefaultAnalyzers.Services;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class KeywordCasingAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics =>
        [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5056Settings>();
        if (settings.KeywordNamingPolicy == KeywordNamingPolicy.Disabled)
        {
            return;
        }

        foreach (var token in script.ParsedScript.ScriptTokenStream)
        {
            AnalyzeToken(context, script, token, settings);
        }
    }

    private static void AnalyzeToken(IAnalysisContext context, IScriptModel script, TSqlParserToken token, Aj5056Settings settings) => AnalyzeKeyword(context, script, token, settings);

    private static void AnalyzeKeyword(IAnalysisContext context, IScriptModel script, TSqlParserToken token, Aj5056Settings settings)
    {
        var shouldBeWrittenAs = KeywordCasingProvider.GetTokenCasing(token.TokenType, settings.KeywordNamingPolicy);
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

        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtToken(token) ?? DatabaseNames.Unknown;
        var fullObjectName = script.ParsedScript
            .TryGetSqlFragmentAtPosition(token)
            ?.TryGetFirstClassObjectName(context, script);

        context.IssueReporter.Report(DiagnosticDefinitions.Default,
            databaseName,
            script.RelativeScriptFilePath,
            fullObjectName,
            token.GetCodeRegion(),
            token.Text, shouldBeWrittenAs, settings.KeywordNamingPolicy.ToString());
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
