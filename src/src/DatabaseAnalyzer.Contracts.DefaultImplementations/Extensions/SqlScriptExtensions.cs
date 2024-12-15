using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class SqlScriptExtensions
{
    public static string? TryGetFullObjectNameAtIndex(this SqlScript script, int characterIndex, string defaultSchemaName)
        => script
            .TryGetCodeObjectAtPosition(characterIndex)
            ?.TryGetFullObjectName(defaultSchemaName);

    public static IParentFragmentProvider CreateParentFragmentProvider(this TSqlScript script)
        => ParentFragmentProviderFactory.Build(script);
}
