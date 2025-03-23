using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Models;

public sealed record ViewColumnInformation(
    string DatabaseName,
    string SchemaName,
    string ViewName,
    string ObjectName,
    TSqlFragment CreationStatement,
    string RelativeScriptFilePath
) : ISchemaBoundObject
{
    public string FullColumnName { get; } = $"{DatabaseName}.{SchemaName}.{ViewName}.{ObjectName}";

    public string FullName { get; } = new[]
    {
        DatabaseName,
        SchemaName,
        ViewName,
        ObjectName
    }.StringJoin('.');

    public IReadOnlyList<string> FullNameParts { get; } = new[]
    {
        DatabaseName,
        SchemaName,
        ViewName,
        ObjectName
    }.ToImmutableArray();
}
