using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class MultiPartIdentifierExtensions
{
    public static string ToUnquotedIdentifier(this MultiPartIdentifier identifier)
        => identifier.Identifiers.Select(a => a.Value).StringJoin(".");

    public static string GetSchemaName(this MultiPartIdentifier identifier, string defaultSchemaName)
        => identifier.Identifiers.Count >= 2
            ? identifier.Identifiers[1].Value
            : defaultSchemaName;

    public static (string? DatabaseName, string? SchemaName, string? ObjectName) GetIdentifierParts(this MultiPartIdentifier identifier)
        => identifier.Count switch
        {
            0 => (null, null, null),
            1 => (null, null, identifier.Identifiers[0].Value),
            2 => (null, identifier.Identifiers[0].Value, identifier.Identifiers[1].Value),
            3 => (identifier.Identifiers[0].Value, identifier.Identifiers[1].Value, identifier.Identifiers[2].Value),
            _ => throw new ArgumentException($"The provided multi-part identifier has more than 3 parts: {identifier.Identifiers.Select(a => a.Value).StringJoin(".")}", nameof(identifier))
        };

    public static (string? TableNameOrAlias, string? ColumnName) GetColumnReferenceParts(this MultiPartIdentifier identifier)
        => identifier.Count switch
        {
            0 => throw new ArgumentException("The provided multi-part identifier for a column is empty", nameof(identifier)),
            1 => (null, identifier.Identifiers[0].Value),
            2 => (identifier.Identifiers[0].Value, identifier.Identifiers[1].Value),
            _ => throw new ArgumentException($"The provided multi-part identifier for a column has more than 2 parts: {identifier.Identifiers.Select(a => a.Value).StringJoin(".")}", nameof(identifier))
        };
}
