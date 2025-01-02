using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public static class CurrentDatabaseNameFinder
{
    public static string FindCurrentDatabaseNameAtFragment(TSqlScript script, TSqlFragment fragment) => FindCurrentDatabaseNameAtLocation(script, fragment.GetCodeLocation());
    public static string FindCurrentDatabaseNameAtToken(TSqlScript script, TSqlParserToken token) => FindCurrentDatabaseNameAtLocation(script, token.GetCodeLocation());

    public static string FindCurrentDatabaseNameAtLocation(TSqlScript script, CodeLocation location)
    {
        var databaseName = TryFindCurrentDatabaseNameAtLocation(script, location);
        if (!databaseName.IsNullOrWhiteSpace())
        {
            return databaseName;
        }

        throw GetUnableToFindDatabaseNameException(script, location);
    }

    public static string? TryFindCurrentDatabaseNameAtFragment(TSqlScript script, TSqlFragment fragment)
    {
        var fragmentLocation = fragment.GetCodeLocation();

        return TryFindCurrentDatabaseNameAtLocation(script, fragmentLocation);
    }

    public static string? TryFindCurrentDatabaseNameAtToken(TSqlScript script, TSqlParserToken token)
    {
        var fragmentLocation = token.GetCodeLocation();

        return TryFindCurrentDatabaseNameAtLocation(script, fragmentLocation);
    }

    public static string? TryFindCurrentDatabaseNameAtLocation(TSqlScript script, CodeLocation location)
    {
        string? currentDatabaseName = null;

        foreach (var child in script.GetChildren(recursive: true))
        {
            if (child is UseStatement useStatement)
            {
                currentDatabaseName = useStatement.DatabaseName.Value;
            }

            var childLocation = child.GetCodeLocation();

            if (childLocation > location)
            {
                return currentDatabaseName;
            }
        }

        return currentDatabaseName;
    }

    private static InvalidOperationException GetUnableToFindDatabaseNameException(TSqlScript script, CodeLocation location)
    {
        var message = $"Unable to determine the database name for the given location {location}."
                      + $" Looks like there's no preceding 'USE' statement. Script content: {script.GetSql()}.";

        throw new InvalidOperationException(message);
    }
}
