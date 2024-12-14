using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class SqlScriptExtensions
{
    public static string? TryGetFullObjectNameAtIndex(this SqlScript script, int characterIndex, string defaultSchemaName)
        => script
            .TryGetCodeObjectAtPosition(characterIndex)
            ?.TryGetFullObjectName(defaultSchemaName);
}
