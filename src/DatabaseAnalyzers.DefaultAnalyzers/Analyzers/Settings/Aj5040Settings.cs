using System.Collections.Frozen;
using System.ComponentModel;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5040SettingsRaw : IRawSettings<Aj5040Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    public IReadOnlyDictionary<string, string?>? BanReasonByFunctionName { get; set; }

    public Aj5040Settings ToSettings() => new
    (
        BanReasonByFunctionName
            .EmptyIfNull()
            .GroupBy(static a => a.Key, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(
                static a => a.Key,
                static a => a.First().Value?.NullIfEmptyOrWhiteSpace() ?? "No reason provided",
                StringComparer.OrdinalIgnoreCase)
    );
}

internal sealed record Aj5040Settings(
    [property: Description("Banned function names. The key is the function name itself and the value is the bann-reason.")]
    IReadOnlyDictionary<string, string> BanReasonByFunctionName
) : ISettings<Aj5040Settings>
{
    public static Aj5040Settings Default { get; } = new(FrozenDictionary<string, string>.Empty);
    public static string DiagnosticId => "AJ5040";
}
