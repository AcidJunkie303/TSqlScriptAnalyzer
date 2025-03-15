using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

#pragma warning disable MA0048 // File name must match type name -> some classes belong to this settings only

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5004")]
internal sealed class Aj5004SettingsRaw : IRawSettings<Aj5004Settings>
{
    public IReadOnlyCollection<TopicAndPatternRaw?>? TopicsAndPatterns { get; set; }

    public Aj5004Settings ToSettings() => new
    (
        TopicsAndPatterns
            .EmptyIfNull()
            .WhereNotNull()
            .Where(a => !a.Topic.IsNullOrWhiteSpace())
            .Where(a => !a.Topic.IsNullOrWhiteSpace())
            .Select(a => new TopicAndPattern(a.Topic!, new Regex(a.Pattern!, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100))))
            .ToImmutableArray()
    );
}

internal sealed class TopicAndPatternRaw
{
    public string? Topic { get; set; }
    public string? Pattern { get; set; }
}

internal sealed record Aj5004Settings(
    [property: Description("An array of objects containing `Topic` and `Pattern` properties.")]
    IReadOnlyCollection<TopicAndPattern> TopicsAndPatterns
) : ISettings<Aj5004Settings>
{
    public static Aj5004Settings Default { get; } = new([]);
    public static string DiagnosticId => "AJ5004";
}

internal sealed record TopicAndPattern(string Topic, Regex Pattern);

#pragma warning restore MA0048
