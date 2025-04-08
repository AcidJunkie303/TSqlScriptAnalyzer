using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts.Settings;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace DatabaseAnalyzer.Common.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ9002")]
public sealed class Aj9002SettingsRaw : IRawDiagnosticSettings<Aj9002Settings>
{
    [Description("Script file path patterns to exclude. Wildcards like `*` and `?` are supported.")]
    public IReadOnlyList<string?>? ExcludedFilePathPatterns { get; set; }

    public Aj9002Settings ToSettings()
    {
        if (ExcludedFilePathPatterns is null)
        {
            return Aj9002Settings.Default;
        }

        var patterns = ExcludedFilePathPatterns
            .WhereNotNull()
            .Select(static a => a.Trim().ToRegexWithSimpleWildcards(caseSensitive: false, compileRegex: true))
            .ToImmutableArray();

        return new Aj9002Settings(patterns);
    }
}

public sealed record Aj9002Settings(
    IReadOnlyList<Regex> ExcludedFilePathPatterns
) : IDiagnosticSettings<Aj9002Settings>
{
    public static Aj9002Settings Default { get; } = new([]);
    public static string DiagnosticId => "AJ9002";
}
