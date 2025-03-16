using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5059")]
internal sealed class Aj5059SettingsRaw : IRawSettings<Aj5059Settings>
{
    public IReadOnlyCollection<string?>? IgnoredProcedureNamePatterns { get; set; }

    public Aj5059Settings ToSettings() => new
    (
        IgnoredProcedureNamePatterns
            .EmptyIfNull()
            .WhereNotNullOrWhiteSpaceOnly()
            .Select(a => a.ToRegexWithSimpleWildcards(caseSensitive: false, compileRegex: true))
            .ToImmutableArray()
    );
}

public sealed record Aj5059Settings(
    [property: Description("Procedure names to ignore. Wildcards like `*` and `?` are supported.")]
    IReadOnlyList<Regex> IgnoredProcedureNamePatterns
) : ISettings<Aj5059Settings>
{
    public static Aj5059Settings Default { get; } = new([]);

    public static string DiagnosticId => "AJ5059";
}
