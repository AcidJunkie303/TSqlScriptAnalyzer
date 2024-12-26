using System.Collections.Immutable;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record SchemaInformationWithObjects(
    string DatabaseName,
    string SchemaName,
    IReadOnlyDictionary<string, TableInformation> TablesByName,
    IReadOnlyDictionary<string, ProcedureInformation> ProceduresByName,
    IReadOnlyDictionary<string, FunctionInformation> FunctionsByName,
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
}
