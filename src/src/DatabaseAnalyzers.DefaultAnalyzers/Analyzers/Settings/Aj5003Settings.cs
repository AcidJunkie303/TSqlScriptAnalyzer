using System.Collections.Immutable;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5003SettingsRaw : IRawSettings<Aj5003Settings>
{
    public IReadOnlyList<string?>? ExcludedFilePathPatterns { get; set; }

    public Aj5003Settings ToSettings()
    {
        if (ExcludedFilePathPatterns is null)
        {
            return Aj5003Settings.Default;
        }

        var patterns = ExcludedFilePathPatterns
            .WhereNotNull()
            .Select(static a => a.Trim().ToRegexWithSimpleWildcards(caseSensitive: false, compileRegex: true))
            .ToImmutableArray();

        return new Aj5003Settings(patterns);
    }
}

internal sealed record Aj5003Settings(
    IReadOnlyList<Regex> ExcludedFilePathPatterns
) : ISettings<Aj5003Settings>
{
    public static Aj5003Settings Default { get; } = new([]);
    public static string DiagnosticId => "AJ5003";
}
