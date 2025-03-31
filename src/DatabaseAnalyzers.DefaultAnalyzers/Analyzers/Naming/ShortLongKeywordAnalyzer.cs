using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class ShortLongKeywordAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;
    private readonly Aj5048Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public ShortLongKeywordAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter, Aj5048Settings settings)
    {
        _context = context;
        _issueReporter = issueReporter;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        if (!_settings.IsEnabled)
        {
            return;
        }

        var violatingNotationsByTokenType = BuildLookupDictionary();

        foreach (var token in _script.ParsedScript.ScriptTokenStream)
        {
            AnalyzeToken(token, violatingNotationsByTokenType);
        }
    }

    private ViolatingNotationsByTokenType BuildLookupDictionary()
    {
        var result = new ViolatingNotationsByTokenType();
        if (_settings.Execute == Aj5048KeywordNotationType.Short)
        {
            result.Add(TSqlTokenType.Execute, (_settings.Execute, "Exec"));
        }

        if (_settings.Execute == Aj5048KeywordNotationType.Long)
        {
            result.Add(TSqlTokenType.Exec, (_settings.Execute, "Execute"));
        }

        if (_settings.Procedure == Aj5048KeywordNotationType.Short)
        {
            result.Add(TSqlTokenType.Procedure, (_settings.Procedure, "Proc"));
        }

        if (_settings.Procedure == Aj5048KeywordNotationType.Long)
        {
            result.Add(TSqlTokenType.Proc, (_settings.Procedure, "Procedure"));
        }

        if (_settings.Transaction == Aj5048KeywordNotationType.Short)
        {
            result.Add(TSqlTokenType.Transaction, (_settings.Transaction, "Tran"));
        }

        if (_settings.Transaction == Aj5048KeywordNotationType.Long)
        {
            result.Add(TSqlTokenType.Tran, (_settings.Transaction, "Transaction"));
        }

        return result;
    }

    private void AnalyzeToken(TSqlParserToken token, ViolatingNotationsByTokenType violatingNotationsByTokenType)
    {
        if (token.Text.IsNullOrWhiteSpace())
        {
            return;
        }

        if (!violatingNotationsByTokenType.TryGetValue(token.TokenType, out var notation))
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
