using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public static class ScriptComplianceChecker
{
    public static bool IsUseDatabaseCalledBeforeAnyOtherStatement(TSqlScript script)
    {
        var children = script.GetChildren()
            .Take(2)
            .ToList();

        if (children.Count == 0)
        {
            return true; // Not actually the case but in this scope, it makes sense
        }

        return children[0] is UserStatement;
    }
}
