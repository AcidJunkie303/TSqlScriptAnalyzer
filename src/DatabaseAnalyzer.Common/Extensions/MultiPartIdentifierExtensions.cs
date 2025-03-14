using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Extensions;

public static class MultiPartIdentifierExtensions
{
    public static string ToUnquotedIdentifier(this MultiPartIdentifier identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        return identifier.Identifiers.Select(static a => a.Value).StringJoin('.');
    }

    public static string GetSchemaName(this MultiPartIdentifier identifier, string defaultSchemaName)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        return identifier.Identifiers.Count >= 2
            ? identifier.Identifiers[0].Value
            : defaultSchemaName;
    }

    public static (string? DatabaseName, string? SchemaName, string ObjectName) GetIdentifierParts(this MultiPartIdentifier identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        return identifier.Count switch
        {
            0 => throw new ArgumentException("The provided multi-part identifier has no elements", nameof(identifier)),
            1 => (null, null, identifier.Identifiers[0].Value!),
            2 => (null, identifier.Identifiers[0].Value, identifier.Identifiers[1].Value!),
            3 => (identifier.Identifiers[0].Value, identifier.Identifiers[1].Value, identifier.Identifiers[2].Value!),
            _ => throw new ArgumentException($"The provided multi-part identifier has more than 3 parts: {identifier.Identifiers.Select(static a => a.Value).StringJoin('.')}", nameof(identifier))
        };
    }

    public static (string? TableNameOrAlias, string ColumnName) GetColumnReferenceParts(this MultiPartIdentifier identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        return identifier.Count switch
        {
            0 => throw new ArgumentException("The provided multi-part identifier for a column is empty", nameof(identifier)),
            1 => (null, identifier.Identifiers[0].Value),
            _ => (identifier.Identifiers[0].Value, identifier.Identifiers[1].Value),
        };
    }
}
