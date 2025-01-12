using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record SchemaInformation(
    string DatabaseName,
    string SchemaName,
    TSqlFragment CreationStatement,
    string RelativeScriptFilePath
) : ISchemaBoundObject
{
    public string ObjectName => SchemaName;

    public IReadOnlyList<string> FullNameParts { get; } = new[]
    {
        DatabaseName,
        SchemaName
    }.ToImmutableArray();

    public string FullName { get; } = new[]
    {
        DatabaseName,
        SchemaName
    }.StringJoin('.');
}
