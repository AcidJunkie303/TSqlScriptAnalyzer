using System.Collections.Frozen;
using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;

public sealed record IndexInformation(
    string DatabaseName,
    string SchemaName,
    string TableName,
    string? IndexName,
    TableColumnIndexTypes IndexType,
    FrozenSet<string> ColumnNames,
    FrozenSet<string> IncludedColumnNames,
    TSqlFragment CreationStatement,
    string RelativeScriptFilePath
) : ISchemaBoundObject
{
    public string ObjectName { get; } = IndexName ?? string.Empty;

    public IReadOnlyList<string> FullNameParts { get; } = new[]
    {
        DatabaseName,
        IndexName ?? string.Empty
    }.ToImmutableArray();

    public string FullName { get; } = new[]
    {
        DatabaseName,
        IndexName ?? string.Empty
    }.StringJoin('.');
}
