using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class ShortLongKeywordAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5048Settings>();

        if (!settings.IsEnabled)
        {
            return;
        }

        var violatingNotationsByTokenType = BuildLookupDictionary(settings);

        foreach (var token in script.ParsedScript.ScriptTokenStream)
        {
            AnalyzeToken(context, script, token, violatingNotationsByTokenType);
        }
    }

    private static ViolatingNotationsByTokenType BuildLookupDictionary(Aj5048Settings settings)
    {
        var result = new ViolatingNotationsByTokenType();
        if (settings.Execute == Aj5048KeywordNotationType.Short)
        {
            result.Add(TSqlTokenType.Execute, (settings.Execute, "Exec"));
        }

        if (settings.Execute == Aj5048KeywordNotationType.Long)
        {
            result.Add(TSqlTokenType.Exec, (settings.Execute, "Execute"));
        }

        if (settings.Procedure == Aj5048KeywordNotationType.Short)
        {
            result.Add(TSqlTokenType.Procedure, (settings.Procedure, "Proc"));
        }

        if (settings.Procedure == Aj5048KeywordNotationType.Long)
        {
            result.Add(TSqlTokenType.Proc, (settings.Procedure, "Procedure"));
        }

        if (settings.Transaction == Aj5048KeywordNotationType.Short)
        {
            result.Add(TSqlTokenType.Transaction, (settings.Transaction, "Tran"));
        }

        if (settings.Transaction == Aj5048KeywordNotationType.Long)
        {
            result.Add(TSqlTokenType.Tran, (settings.Transaction, "Transaction"));
        }

        return result;
    }

    private static void AnalyzeToken(IAnalysisContext context, IScriptModel script, TSqlParserToken token, ViolatingNotationsByTokenType violatingNotationsByTokenType)
    {
        if (token.Text.IsNullOrWhiteSpace())
        {
            return;
        }

        if (!violatingNotationsByTokenType.TryGetValue(token.TokenType, out var notation))
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
            token.Text, notation.NotationType.ToString(), notation.ShouldBeWrittenAs);
    }

    private sealed class ViolatingNotationsByTokenType : Dictionary<TSqlTokenType, (Aj5048KeywordNotationType NotationType, string ShouldBeWrittenAs)>
    {
        public ViolatingNotationsByTokenType() : base(3) // we'll have max 3 items in this dictionary
        {
        }
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
