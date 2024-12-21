using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public static class CurrentDatabaseNameFinder
{
    public static string FindCurrentDatabaseNameAtFragment(TSqlFragment fragment, TSqlScript script)
    {
        var visitor = new Visitor(fragment);
        visitor.ExplicitVisit(script);

        if (visitor.DatabaseNameAtFragment is not null)
        {
            return visitor.DatabaseNameAtFragment;
        }

        var message = "Unable to determine the database name for the given fragment."
                      + $" Looks like there's no preceding 'USE' statement. Script content: {script.GetSql()}. Fragment code region: {fragment.GetCodeRegion()}";

        throw new InvalidOperationException(message);
    }

    private sealed class Visitor : DatabaseAwareFragmentVisitor
    {
        private readonly TSqlFragment _fragmentToCheckCurrentDatabaseName;

        public string? DatabaseNameAtFragment { get; private set; }

        public Visitor(TSqlFragment fragmentToCheckCurrentDatabaseName) : base("dbo") // defaultSchemaName is not necessary here, so we use dbo
        {
            _fragmentToCheckCurrentDatabaseName = fragmentToCheckCurrentDatabaseName;
        }

        public override void Visit(TSqlFragment fragment)
        {
            if (ReferenceEquals(_fragmentToCheckCurrentDatabaseName, fragment))
            {
                DatabaseNameAtFragment = CurrentDatabaseName;
                return;
            }

            fragment.AcceptChildren(this);
        }
    }
}
