using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed class ColumnInformationRaw
{
    public string? Name { get; set; }

    public bool IsNullable { get; set; }
    public DataTypeReference? DataType { get; set; }
    public bool IsUnique { get; set; }
}

public sealed record ColumnInformation(
    string DatabaseName,
    string SchemaName,
    string TableName,
    string ColumnName,
    bool IsNullable,
    bool IsUnique,
    DataTypeReference DataType
) : ISchemaBoundObject;
