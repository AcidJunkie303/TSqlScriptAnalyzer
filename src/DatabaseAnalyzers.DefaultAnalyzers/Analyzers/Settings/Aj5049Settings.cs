using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5049")]
internal sealed class Aj5049SettingsRaw : IRawDiagnosticSettings<Aj5049Settings>
{
    public IReadOnlyCollection<string?>? IgnoredObjectNamePatterns { get; set; }

    public Aj5049Settings ToSettings() => new
    (
        IgnoredObjectNamePatterns
            .EmptyIfNull()
            .WhereNotNull()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(a => a.ToRegexWithSimpleWildcards(caseSensitive: false, compileRegex: true))
            .ToImmutableArray()
    );
}

public sealed record Aj5049Settings(
    [property: Description("Object names to exclude. Wildcards like `*` and `?` are supported.")]
    IReadOnlyList<Regex> IgnoredObjectNamePatterns
) : IDiagnosticSettings<Aj5049Settings>
{
    public static Aj5049Settings Default { get; } = new([]);
    public static string DiagnosticId => "AJ5049";
}
