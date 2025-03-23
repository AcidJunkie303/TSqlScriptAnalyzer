using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;
using DatabaseAnalyzer.Common.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5003")]
internal sealed class Aj5003SettingsRaw : IRawDiagnosticSettings<Aj5003Settings>
{
    [Description("Script file path patterns to exclude. Wildcards like `*` and `?` are supported.")]
    public IReadOnlyList<string?>? ExcludedFilePathPatterns { get; set; }

    public Aj5003Settings ToSettings()
    {
        if (ExcludedFilePathPatterns is null)
        {
            return Aj5003Settings.Default;
        }

        var patterns = ExcludedFilePathPatterns
            .WhereNotNull()
            .Select(static a => a.Trim().ToRegexWithSimpleWildcards(caseSensitive: false, compileRegex: true))
            .ToImmutableArray();

        return new Aj5003Settings(patterns);
    }
}

public sealed record Aj5003Settings(
    IReadOnlyList<Regex> ExcludedFilePathPatterns
) : IDiagnosticSettings<Aj5003Settings>
{
    public static Aj5003Settings Default { get; } = new([]);
    public static string DiagnosticId => "AJ5003";
}
