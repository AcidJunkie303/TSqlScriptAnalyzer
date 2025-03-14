using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing.Extraction;

internal sealed class ObjectExtractorVisitor<T> : DatabaseAwareFragmentVisitor
    where T : TSqlFragment
{
    public ObjectExtractorVisitor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    public List<(string? DatabaseName, T Object)> Objects { get; } = [];

    public override void Visit(TSqlFragment fragment)
    {
        if (fragment is T t)
        {
            Objects.Add((CurrentDatabaseName, t));
        }

        base.Visit(fragment);
    }
}
