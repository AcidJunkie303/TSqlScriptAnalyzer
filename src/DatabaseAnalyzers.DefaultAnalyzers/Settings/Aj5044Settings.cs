using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;
using DatabaseAnalyzer.Common.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5044")]
internal sealed class Aj5044SettingsRaw : IRawDiagnosticSettings<Aj5044Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    [Description("Object names to ignore. Wildcards like `*` and `?` are supported.")]
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

public sealed record Aj5044Settings(
    IReadOnlyList<Regex> IgnoredObjectNamePatterns
) : IDiagnosticSettings<Aj5044Settings>
{
    public static Aj5044Settings Default { get; } = new([]);
    public static string DiagnosticId => "AJ5044";
}
