using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;

internal sealed class ObjectExtractorVisitor<T> : DatabaseAwareConcreteFragmentVisitor
    where T : TSqlFragment
{
    public List<(string? DatabaseName, T Object)> Objects { get; } = [];

    public ObjectExtractorVisitor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    public override void Visit(TSqlFragment fragment)
    {
        if (fragment is T t)
        {
            Objects.Add((CurrentDatabaseName, t));
        }

        base.Visit(fragment);
    }
}
