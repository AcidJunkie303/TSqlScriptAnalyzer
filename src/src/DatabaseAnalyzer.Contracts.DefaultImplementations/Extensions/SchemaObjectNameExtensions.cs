using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class SchemaObjectNameExtensions
{
    public static (string SchemaName, string ObjectName) GetTwoPartObjectName(this SchemaObjectName objectName, string defaultSchemaName)
    {
        return objectName.SchemaIdentifier.Value.IsNullOrWhiteSpace()
            ? (defaultSchemaName, objectName.BaseIdentifier.Value)
            : (objectName.SchemaIdentifier.Value, objectName.BaseIdentifier.Value);
    }

    public static string GetConcatenatedTwoPartObjectName(this SchemaObjectName objectName, string defaultSchemaName)
    {
        return (objectName.SchemaIdentifier?.Value).IsNullOrWhiteSpace()
            ? $"{defaultSchemaName}.{objectName.BaseIdentifier.Value}"
            : $"{objectName.SchemaIdentifier.Value}.{objectName.BaseIdentifier.Value}";
    }
}
