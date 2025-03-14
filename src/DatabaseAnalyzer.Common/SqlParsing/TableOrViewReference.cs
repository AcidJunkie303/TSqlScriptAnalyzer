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
}
