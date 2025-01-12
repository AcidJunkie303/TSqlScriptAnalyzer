using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed class OpenItemAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5004Settings>();
        var commentTokens = script.ParsedScript.ScriptTokenStream
            .Where(a => a.TokenType is TSqlTokenType.SingleLineComment or TSqlTokenType.MultilineComment);

        foreach (var commentToken in commentTokens)
        {
            AnalyzeToken(context, script, settings, commentToken);
        }
    }

    private static void AnalyzeToken(IAnalysisContext context, IScriptModel script, Aj5004Settings settings, TSqlParserToken commentToken)
    {
        foreach (var (topic, expression) in settings.TopicsAndPatterns)
        {
            var match = expression.Match(commentToken.Text);
            if (!match.Success)
            {
                continue;
            }

            var message = match.Groups.TryGetValue("message", out var group)
                ? group.Value
                : "Unable to parse message. Make sure that the regex pattern contain a named group capture called 'message'.";

            var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(script.ParsedScript) ?? script.DatabaseName;
            var fullObjectName = script.ParsedScript
                .TryGetSqlFragmentAtPosition(commentToken)
                ?.TryGetFirstClassObjectName(context, script);

            context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, commentToken.GetCodeRegion(), topic, message);
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
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
