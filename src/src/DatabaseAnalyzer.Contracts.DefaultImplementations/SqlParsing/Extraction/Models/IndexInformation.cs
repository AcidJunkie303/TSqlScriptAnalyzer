using System.Collections.Frozen;
using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record IndexInformation(
    string DatabaseName,
    string SchemaName,
    string TableName,
    string? IndexName,
    TableColumnIndexType IndexType,
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
