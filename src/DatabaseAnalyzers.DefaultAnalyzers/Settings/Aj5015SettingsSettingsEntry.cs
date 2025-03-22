using System.Text.RegularExpressions;
using Ardalis.GuardClauses;
using DatabaseAnalyzer.Common.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

public sealed class Aj5015SettingsSettingsEntryRaw
{
    public string? FullColumnNamePattern { get; set; }
    public string? SuppressionReason { get; set; }

    public Aj5015SettingsSettingsEntry ToSettings() => new
    (
        Guard.Against.NullOrWhiteSpace(FullColumnNamePattern).ToRegexWithSimpleWildcards(caseSensitive: false, compileRegex: true),
        Guard.Against.NullOrWhiteSpace(SuppressionReason)
    );
}

public sealed record Aj5015SettingsSettingsEntry(Regex FullColumnNamePattern, string SuppressionReason);
