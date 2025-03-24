using System.ComponentModel;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;
using DatabaseAnalyzer.Common.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5066")]
internal sealed class Aj5066SettingsRaw : IRawDiagnosticSettings<Aj5066Settings>
{
    [Description("Table names to ignore. Wildcards like `*` and `?` are supported.")]
    public IReadOnlyCollection<string?>? IgnoredTableNames { get; set; }

    public Aj5066Settings ToSettings() => new
    (
        IgnoredTableNames
            .EmptyIfNull()
            .WhereNotNullOrWhiteSpaceOnly()
            .Select(static a => a.ToRegexWithSimpleWildcards(caseSensitive: false, compileRegex: true))
            .ToList()
    );
}

public sealed record Aj5066Settings(
    IReadOnlyList<Regex> IgnoredTableNames
) : IDiagnosticSettings<Aj5066Settings>
{
    public static Aj5066Settings Default { get; } = new([]);

    public static string DiagnosticId => "AJ5066";
}
