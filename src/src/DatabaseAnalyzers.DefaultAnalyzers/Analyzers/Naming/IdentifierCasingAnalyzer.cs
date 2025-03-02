using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class IdentifierCasingAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics =>
        [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5057Settings>();
        if (settings.CasingByIdentifier.Count == 0)
        {
            return;
        }

        foreach (var token in script.ParsedScript.ScriptTokenStream)
        {
            if (token.TokenType == TSqlTokenType.Identifier)
            {
                AnalyzeIdentifier(context, script, token, settings);
                return;
            }
        }
    }

    private static void AnalyzeIdentifier(IAnalysisContext context, IScriptModel script, TSqlParserToken token, Aj5057Settings settings)
    {
        if (token.Text.IsNullOrWhiteSpace())
        {
            return;
        }

        var shouldBeWrittenAs = settings.CasingByIdentifier.GetValueOrDefault(token.Text);
        if (shouldBeWrittenAs is null || token.Text.EqualsOrdinal(shouldBeWrittenAs))
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
