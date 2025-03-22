using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;
using DatabaseAnalyzer.Common.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5026")]
internal sealed class Aj5026SettingsRaw : IRawDiagnosticSettings<Aj5026Settings>
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

public sealed record Aj5026Settings(
    [property: Description("Full table names (`database`.`schema`.`table`) to ignore. Wildcards like `*` and `?` are supported.")]
    IReadOnlyList<Regex> FullTableNamesToIgnore
) : IDiagnosticSettings<Aj5026Settings>
{
    public static Aj5026Settings Default { get; } = new([]);
    public static string DiagnosticId => "AJ5026";
}
