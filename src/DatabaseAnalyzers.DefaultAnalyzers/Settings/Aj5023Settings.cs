using System.Collections.Frozen;
using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5023")]
internal sealed class Aj5023SettingsRaw : IRawDiagnosticSettings<Aj5023Settings>
{
    public IReadOnlyCollection<string?>? StatementTypesToIgnore { get; set; }

    public Aj5023Settings ToSettings() => new
    (
        StatementTypesToIgnore.EmptyIfNull()
            .WhereNotNull()
            .Select(static a => Enum.TryParse<TSqlTokenType>(a, ignoreCase: true, out var tokenType)
                ? tokenType
                : (TSqlTokenType?) null)
            .Where(static a => a.HasValue)
            .Select(static a => a!.Value)
            .ToFrozenSet()
    );
}

public sealed record Aj5023Settings(
    [property: Description("Statement type names to ignore.")]
    FrozenSet<TSqlTokenType> StatementTypesToIgnore
) : IDiagnosticSettings<Aj5023Settings>
{
    public static Aj5023Settings Default { get; } = new(FrozenSet<TSqlTokenType>.Empty);
    public static string DiagnosticId => "AJ5023";
}
