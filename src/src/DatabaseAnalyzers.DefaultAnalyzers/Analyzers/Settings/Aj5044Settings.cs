using System.Collections.Immutable;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5044SettingsRaw : IRawSettings<Aj5044Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    public IReadOnlyList<string?>? IgnoredObjectNamePatterns { get; set; }

    public Aj5044Settings ToSettings() => new
    (
        IgnoredObjectNamePatterns
            .EmptyIfNull()
            .WhereNotNull()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(a => a.ToRegexWithSimpleWildcards(caseSensitive: false, compileRegex: true))
            .ToImmutableArray()
    );
}

internal sealed record Aj5044Settings(
    IReadOnlyList<Regex> IgnoredObjectNamePatterns
) : ISettings<Aj5044Settings>
{
    public static Aj5044Settings Default { get; } = new([]);
    public static string DiagnosticId => "AJ5044";
}
