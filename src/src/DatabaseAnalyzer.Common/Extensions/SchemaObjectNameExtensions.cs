using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Extensions;

public static class SchemaObjectNameExtensions
{
    public static (string SchemaName, string ObjectName) GetTwoPartObjectName(this SchemaObjectName objectName, string defaultSchemaName)
    {
        ArgumentNullException.ThrowIfNull(objectName);

        return (objectName.SchemaIdentifier?.Value).IsNullOrWhiteSpace()
            ? (defaultSchemaName, objectName.BaseIdentifier.Value)
            : (objectName.SchemaIdentifier.Value, objectName.BaseIdentifier.Value);
    }

    public static string GetConcatenatedTwoPartObjectName(this SchemaObjectName objectName, string defaultSchemaName)
    {
        ArgumentNullException.ThrowIfNull(objectName);

        return (objectName.SchemaIdentifier?.Value).IsNullOrWhiteSpace()
            ? $"{defaultSchemaName}.{objectName.BaseIdentifier.Value}"
            : $"{objectName.SchemaIdentifier.Value}.{objectName.BaseIdentifier.Value}";
    }

    public static bool Equals(this SchemaObjectName name, SchemaObjectName other, string defaultSchemaName)
        => string.Equals(name.ServerIdentifier?.Value, other.ServerIdentifier?.Value, StringComparison.OrdinalIgnoreCase)
           && string.Equals(name.DatabaseIdentifier?.Value, other.DatabaseIdentifier?.Value, StringComparison.OrdinalIgnoreCase)
           && string.Equals(name.SchemaIdentifier?.Value ?? defaultSchemaName, other.SchemaIdentifier?.Value ?? defaultSchemaName, StringComparison.OrdinalIgnoreCase)
           && string.Equals(name.BaseIdentifier?.Value, other.BaseIdentifier?.Value, StringComparison.OrdinalIgnoreCase);
}
