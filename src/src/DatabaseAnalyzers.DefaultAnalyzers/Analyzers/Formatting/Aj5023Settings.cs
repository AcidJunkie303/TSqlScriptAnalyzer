using System.Collections.Frozen;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class Aj5023SettingsRaw
{
    public IReadOnlyCollection<string?>? StatementTypesToIgnore { get; set; }

    public Aj5023Settings ToSettings() => new
    (
        StatementTypesToIgnore.EmptyIfNull()
            .WhereNotNull()
            .Select(a => Enum.TryParse<TSqlTokenType>(a, true, out var tokenType)
                ? tokenType
                : (TSqlTokenType?)null)
            .Where(a => a.HasValue)
            .Select(a => a!.Value)
            .ToFrozenSet()
    );
}

public sealed record Aj5023Settings(
    FrozenSet<TSqlTokenType> StatementTypesToIgnore
) : ISettings<Aj5023Settings>
{
    public static Aj5023Settings Default { get; } = new([]);
    public static string DiagnosticId => "AJ5023";
}
