using System.Reflection;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Services;

internal static class Aj5058Helpers
{
    public static IEnumerable<(string ShortenedName, Type Type)> GetDropStatementTypes()
    {
        const string statement = "Statement";

        return Assembly
            .GetAssembly(typeof(DropTableStatement))!
            .GetTypes()
            .Where(static a => a is { IsPublic: true, IsAbstract: false, IsInterface: false })
            .Where(IsStatement)
            .Where(static a => a.Name.Contains("Drop", StringComparison.Ordinal))
            .Where(static a => a.Name.EndsWith(statement, StringComparison.Ordinal))
            .Select(static a => (ShortenedName: a.Name[..^statement.Length], Type: a));
    }

    private static bool IsStatement(Type? type)
    {
        if (type is null)
        {
            return false;
        }

        return type == typeof(TSqlStatement) || IsStatement(type.BaseType);
    }
}
