using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class KeywordCasingAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics =>
        [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5048Settings>();
        if (settings.KeywordNamingPolicy == KeywordNamingPolicy.Disabled)
        {
            return;
        }

        var keywordCasingByType = CachedKeywordCasingAnalyzerSettingsProvider.Get(settings.KeywordNamingPolicy);

        foreach (var token in script.ParsedScript.ScriptTokenStream)
        {
            if (settings.ExcludedKeywordTokenTypes.Contains(token.TokenType))
            {
                continue;
            }

            AnalyzeToken(context, script, keywordCasingByType, settings.KeywordNamingPolicy, token);
        }
    }

    private static void AnalyzeToken(IAnalysisContext context, IScriptModel script, FrozenDictionary<TSqlTokenType, string> keywordCasingByType, KeywordNamingPolicy keywordNamingPolicy, TSqlParserToken token)
    {
        if (!ContainsCharacters(token.Text))
        {
            return;
        }

        if (!keywordCasingByType.TryGetValue(token.TokenType, out var shouldBeWrittenAs))
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
            token.Text, shouldBeWrittenAs, keywordNamingPolicy.ToString());
    }

    private static bool ContainsCharacters(string? value)
        => value?.Any(char.IsLetter) == true;

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5048",
            IssueType.Formatting,
            "Keyword uses wrong casing",
            "The keyword `{0}` should be written as `{1}` because of the configured keyword casing policy `{2}`.",
            ["The actual used keyword", "The expected keyword casing", "The configure keyword casing policy"],
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}

[SuppressMessage("Design", "MA0048:File name must match type name")]
internal static class CachedKeywordCasingAnalyzerSettingsProvider
{
    private static readonly ConcurrentDictionary<KeywordNamingPolicy, FrozenDictionary<TSqlTokenType, string>> Cache = new();

    public static FrozenDictionary<TSqlTokenType, string> Get(KeywordNamingPolicy policy) => Cache.GetOrAdd(policy, Create);

    private static FrozenDictionary<TSqlTokenType, string> Create(KeywordNamingPolicy policy)
    {
        var transformer = GetTransformer(policy);
        return Enum
            .GetValues<TSqlTokenType>()
            .ToFrozenDictionary(a => a, a => transformer(a.ToString()!));
    }

    private static Func<string, string> GetTransformer(KeywordNamingPolicy policy)
        => policy switch
        {
            KeywordNamingPolicy.Disabled => TransformToPascalCasing,
            KeywordNamingPolicy.UpperCase => TransformToUpperCase,
            KeywordNamingPolicy.LowerCase => TransformToLowerCase,
            KeywordNamingPolicy.CamelCase => TransformToCamelCasing,
            KeywordNamingPolicy.PascalCase => TransformToPascalCasing,
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, $"Value {policy} is not handled")
        };

    private static string TransformToPascalCasing(string value) => value;

    [SuppressMessage("Minor Code Smell", "S4040:Strings should be normalized to uppercase")]
    private static string TransformToCamelCasing(string value)
        => string.Concat(char.ToLowerInvariant(value[0]), value[1..]);

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
    [SuppressMessage("Minor Code Smell", "S4040:Strings should be normalized to uppercase")]
    private static string TransformToLowerCase(string value)
        => value.ToLowerInvariant();

    private static string TransformToUpperCase(string value)
        => value.ToUpperInvariant();
}
