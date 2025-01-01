using System.Collections.Immutable;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5027SettingsRaw : IRawSettings<Aj5027Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    public IReadOnlyList<string>? FullTableNamesToIgnore { get; set; }

    public Aj5027Settings ToSettings() => new
    (
        FullTableNamesToIgnore
            .EmptyIfNull()
            .WhereNotNullOrWhiteSpaceOnly()
            .Select(static a => a.ToRegexWithSimpleWildcards(caseSensitive: false, compileRegex: true))
            .ToImmutableArray()
    );
}

internal sealed record Aj5027Settings(
    IReadOnlyList<Regex> FullTableNamesToIgnore
) : ISettings<Aj5027Settings>
{
    public static Aj5027Settings Default { get; } = new([]);
    public static string DiagnosticId => "AJ5027";
}
