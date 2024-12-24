using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;

internal abstract class Extractor<T>
{
    protected Extractor(string defaultSchemaName)
    {
        DefaultSchemaName = defaultSchemaName;
    }

    protected string DefaultSchemaName { get; }

    public IEnumerable<T> Extract(IEnumerable<IScriptModel> scripts)
        => scripts.SelectMany(Extract);

    public IEnumerable<T> Extract(IEnumerable<TSqlScript> scripts)
        => scripts.SelectMany(Extract);

    public IReadOnlyList<T> Extract(IScriptModel script)
        => ExtractCore(script.ParsedScript);

    public IReadOnlyList<T> Extract(TSqlScript script)
        => ExtractCore(script);

    protected abstract List<T> ExtractCore(TSqlScript script);

    protected static InvalidOperationException CreateUnableToDetermineTheDatabaseNameException(string objectType, string objectName, CodeRegion codeRegion)
        => new($"Unable to determine the database name for {objectType} '{objectName}' because neither the object creation statement nor the script contains a preceding 'USE <db-name>' statement. Location: {codeRegion}");
}
