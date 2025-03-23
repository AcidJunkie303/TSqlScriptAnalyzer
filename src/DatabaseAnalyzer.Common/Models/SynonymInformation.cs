using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Models;

public sealed record SynonymInformation(
    string DatabaseName,
    string SchemaName,
    string ObjectName,
    TSqlFragment CreationStatement,
    string RelativeScriptFilePath,
    string? TargetServerName,
    string? TargetDatabaseName,
    string TargetSchemaName,
    string TargetObjectName
) : ISchemaBoundObject
{
    public required IScriptModel ScriptModel { get; init; }
    public string FullName { get; } = $"{DatabaseName}.{SchemaName}.{ObjectName}";

    public IReadOnlyList<string> FullNameParts { get; } = new[]
    {
        DatabaseName,
        SchemaName,
        ObjectName
    }.ToImmutableArray();
}
