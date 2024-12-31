using System.Collections.Immutable;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5026SettingsRaw : IRawSettings<Aj5026Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    public IReadOnlyList<string>? FullTableNamesToIgnore { get; set; }

    public Aj5026Settings ToSettings() => new
    (
        FullTableNamesToIgnore
            .EmptyIfNull()
            .WhereNotNullOrWhiteSpaceOnly()
            .Select(static a => a.ToRegexWithSimpleWildcards(compileRegex: true))
            .ToImmutableArray()
    );
}

internal sealed record Aj5026Settings(
    IReadOnlyList<Regex> FullTableNamesToIgnore
) : ISettings<Aj5026Settings>
{
    public static Aj5026Settings Default { get; } = new([]);
    public static string DiagnosticId => "AJ5026";
}
