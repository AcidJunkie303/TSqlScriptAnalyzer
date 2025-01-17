using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

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
    [property: Description("Script file path patterns to exclude. Wildcards like `*` and `?` are supported.")]
    IReadOnlyList<Regex> ExcludedFilePathPatterns
) : ISettings<Aj5003Settings>
{
    public static Aj5003Settings Default { get; } = new([]);
    public static string DiagnosticId => "AJ5003";
}
