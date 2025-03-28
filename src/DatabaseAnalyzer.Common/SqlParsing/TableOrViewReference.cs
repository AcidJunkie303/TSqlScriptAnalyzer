using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing;

public sealed record TableOrViewReference(
    string DatabaseName,
    string SchemaName,
    string ObjectName,
    TableSourceType SourceType,
    TSqlFragment Fragment,
    string UsedIn
)
{
    public string FullName { get; } = $"{DatabaseName}.{SchemaName}.{ObjectName}";

    public static TableOrViewReference MissingAliasTableReference { get; } = new
    (
        string.Empty,
        string.Empty,
        string.Empty,
        TableSourceType.Unknown,
        new NamedTableReference(),
        string.Empty
    );
}
