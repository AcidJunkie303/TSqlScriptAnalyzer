using System.Collections.Frozen;
using System.ComponentModel;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5060SettingsRaw : IRawSettings<Aj5060Settings>
{
    public IReadOnlyCollection<string?>? ReservedIdentifierNames { get; set; }

    public Aj5060Settings ToSettings() => new
    (
        ReservedIdentifierNames
            .EmptyIfNull()
            .WhereNotNullOrWhiteSpaceOnly()
            .SelectMany(a => a.Split(';', StringSplitOptions.RemoveEmptyEntries))
            .Where(a => a.Length > 0)
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase)
    );
}

internal sealed record Aj5060Settings(
    [property: Description("Reserved words / keywords to report for table, view, procedure and function names. The can be defined in one or more string where each word is separated by a semicolon.")]
    FrozenSet<string> ReservedIdentifierNames
) : ISettings<Aj5060Settings>
{
    public static Aj5060Settings Default { get; } = new([]);

    public static string DiagnosticId => "AJ5060";
}
