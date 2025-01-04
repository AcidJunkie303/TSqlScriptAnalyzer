using System.Collections.Frozen;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5023SettingsRaw : IRawSettings<Aj5023Settings>
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

internal sealed record Aj5023Settings(
    FrozenSet<TSqlTokenType> StatementTypesToIgnore
) : ISettings<Aj5023Settings>
{
    public static Aj5023Settings Default { get; } = new([]);
    public static string DiagnosticId => "AJ5023";
}
