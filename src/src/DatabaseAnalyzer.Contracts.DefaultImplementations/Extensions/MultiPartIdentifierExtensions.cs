using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class MultiPartIdentifierExtensions
{
    public static string ToUnquotedIdentifier(this MultiPartIdentifier identifier)
        => identifier.Identifiers.Select(a => a.Value).StringJoin(".");
}
