using System.Collections.Frozen;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5004SettingsRaw : IRawSettings<Aj5004Settings>
{
    public IReadOnlyDictionary<string, string?>? TopicsByPattern { get; set; }

    public Aj5004Settings ToSettings() => new
    (
        TopicsByPattern
            .EmptyIfNull()
            .Where(a => !a.Value.IsNullOrWhiteSpace())
            .ToFrozenDictionary(
                a => new Regex(a.Key, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100)),
                a => a.Value!)
    );
}

internal sealed record Aj5004Settings(
    [property: Description("The regular expressions to check comments for. The dictionary key is the regular expression and the value is the topic. Both, message and topic will be reported to the issue raised.")]
    IReadOnlyDictionary<Regex, string> TopicsByPattern
) : ISettings<Aj5004Settings>
{
    public static Aj5004Settings Default { get; } = new(FrozenDictionary<Regex, string>.Empty);
    public static string DiagnosticId => "AJ5004";
}
