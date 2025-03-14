using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

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
            .Select(static a => a.ToRegexWithSimpleWildcards(caseSensitive: false, compileRegex: true))
            .ToImmutableArray()
    );
}

internal sealed record Aj5026Settings(
    [property: Description("Full table names (`database`.`schema`.`table`) to ignore. Wildcards like `*` and `?` are supported.")]
    IReadOnlyList<Regex> FullTableNamesToIgnore
) : ISettings<Aj5026Settings>
{
    public static Aj5026Settings Default { get; } = new([]);
    public static string DiagnosticId => "AJ5026";
}
