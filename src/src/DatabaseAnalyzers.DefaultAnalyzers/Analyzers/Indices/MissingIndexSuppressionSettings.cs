using System.Text.RegularExpressions;
using Ardalis.GuardClauses;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class MissingIndexSuppressionSettingsRaw
{
    public string? FullColumnNamePattern { get; set; }
    public string? SuppressionReason { get; set; }

    public MissingIndexSuppressionSettings ToSettings() => new
    (
        Guard.Against.NullOrWhiteSpace(FullColumnNamePattern).ToRegexWithSimpleWildcards(caseSensitive: false, compileRegex: true),
        Guard.Against.NullOrWhiteSpace(SuppressionReason)
    );
}

public sealed record MissingIndexSuppressionSettings(Regex FullColumnNamePattern, string SuppressionReason);
