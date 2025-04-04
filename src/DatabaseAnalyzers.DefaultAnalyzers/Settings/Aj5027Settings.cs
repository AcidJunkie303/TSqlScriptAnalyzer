using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;
using DatabaseAnalyzer.Common.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5027")]
internal sealed class Aj5027SettingsRaw : IRawDiagnosticSettings<Aj5027Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    [Description("Full table names (`database`.`schema`.`table`) to ignore. Wildcards like `*` and `?` are supported.")]
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

public sealed record Aj5027Settings(
    IReadOnlyList<Regex> FullTableNamesToIgnore
) : IDiagnosticSettings<Aj5027Settings>
{
    public static Aj5027Settings Default { get; } = new([]);
    public static string DiagnosticId => "AJ5027";
}
