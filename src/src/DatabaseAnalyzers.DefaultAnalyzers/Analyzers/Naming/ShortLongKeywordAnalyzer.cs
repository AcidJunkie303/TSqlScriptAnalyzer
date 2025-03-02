using System.Collections.Frozen;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class ShortLongKeywordAnalyzer : IScriptAnalyzer
{
    private static readonly FrozenDictionary<(TSqlTokenType, Aj5048KeywordNotationType), string> EnforcedNotationByTokenTypeAndNotationType = new[]
    {
        KeyValuePair.Create((TSqlTokenType.Proc, Aj5048KeywordNotationType.Long), "Procedure"),
        KeyValuePair.Create((TSqlTokenType.Procedure, Aj5048KeywordNotationType.Short), "Proc"),
        KeyValuePair.Create((TSqlTokenType.Exec, Aj5048KeywordNotationType.Long), "Execute"),
        KeyValuePair.Create((TSqlTokenType.Execute, Aj5048KeywordNotationType.Short), "Exec"),
        KeyValuePair.Create((TSqlTokenType.Tran, Aj5048KeywordNotationType.Long), "Transaction"),
        KeyValuePair.Create((TSqlTokenType.Transaction, Aj5048KeywordNotationType.Short), "Tran")
    }.ToFrozenDictionary();

    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5048Settings>();
        if (settings.KeywordNotationType == Aj5048KeywordNotationType.None)
        {
            return;
        }

        foreach (var token in script.ParsedScript.ScriptTokenStream)
        {
            AnalyzeToken(context, script, token, settings);
        }
    }

    private static void AnalyzeToken(IAnalysisContext context, IScriptModel script, TSqlParserToken token, Aj5048Settings settings)
    {
        if (token.Text.IsNullOrWhiteSpace())
        {
            return;
        }

        if (!EnforcedNotationByTokenTypeAndNotationType.TryGetValue((token.TokenType, settings.KeywordNotationType), out var notation))
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
            token.Text, settings.KeywordNotationType.ToString(), notation);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5048",
            IssueType.Formatting,
            "Long/Short keyword notation violation",
            "The keyword `{0}` should use the `{1}` notation `{2}`.",
            ["The keyword", "Short or Long", "The enforced notation"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
