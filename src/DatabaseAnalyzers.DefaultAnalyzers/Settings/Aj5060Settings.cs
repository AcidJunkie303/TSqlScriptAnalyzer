using System.Collections.Frozen;
using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;
using DatabaseAnalyzer.Common.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5060")]
internal sealed class Aj5060SettingsRaw : IRawDiagnosticSettings<Aj5060Settings>
{
    [Description("Reserved words / keywords to report for table, view, column, procedure and function names. They can be defined in one or more strings (array) where each word is separated by a semicolon.")]
    public IReadOnlyCollection<string?>? ReservedIdentifierNames { get; set; }

    public Aj5060Settings ToSettings() => new
    (
        ReservedIdentifierNames
            .EmptyIfNull()
            .WhereNotNullOrWhiteSpaceOnly()
            .SelectMany(static a => a.Split(';', StringSplitOptions.RemoveEmptyEntries))
            .Where(static a => a.Length > 0)
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase)
    );
}

public sealed record Aj5060Settings(
    FrozenSet<string> ReservedIdentifierNames
) : IDiagnosticSettings<Aj5060Settings>
{
    public static Aj5060Settings Default { get; } = new(FrozenSet<string>.Empty);

    public static string DiagnosticId => "AJ5060";
}
