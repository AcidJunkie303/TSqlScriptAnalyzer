using System.Collections.Frozen;
using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Models;

public sealed record ViewInformation(
    string DatabaseName,
    string SchemaName,
    string ObjectName,
    IReadOnlyList<ViewColumnInformation> Columns,
    TSqlFragment CreationStatement,
    string RelativeScriptFilePath
) : ISchemaBoundObject
{
    public required IScriptModel ScriptModel { get; init; }
    public string FullName { get; } = $"{DatabaseName}.{SchemaName}.{ObjectName}";
    public IReadOnlyDictionary<string, ViewColumnInformation> ColumnsByName { get; } = Columns.ToFrozenDictionary(a => a.ObjectName, a => a, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> FullNameParts { get; } = new[]
    {
        DatabaseName,
        SchemaName,
        ObjectName
    }.ToImmutableArray();
}
