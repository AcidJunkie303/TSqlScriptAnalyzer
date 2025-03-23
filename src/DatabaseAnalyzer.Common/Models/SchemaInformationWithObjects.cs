using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Models;

public sealed record SchemaInformationWithObjects(
    string DatabaseName,
    string SchemaName,
    IReadOnlyDictionary<string, TableInformation> TablesByName,
    IReadOnlyDictionary<string, ViewInformation> ViewsByName,
    IReadOnlyDictionary<string, ProcedureInformation> ProceduresByName,
    IReadOnlyDictionary<string, FunctionInformation> FunctionsByName,
    IReadOnlyDictionary<string, SynonymInformation> SynonymsByName,
    TSqlFragment CreationStatement,
    string RelativeScriptFilePath
) : ISchemaBoundObject
{
    public string ObjectName => SchemaName;

    public string FullName { get; } = new[]
    {
        DatabaseName,
        SchemaName
    }.StringJoin('.');

    public IReadOnlyList<string> FullNameParts { get; } = new[]
    {
        DatabaseName,
        SchemaName
    }.ToImmutableArray();
}
