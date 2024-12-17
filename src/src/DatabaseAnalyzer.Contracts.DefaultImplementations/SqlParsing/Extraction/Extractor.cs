using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;

internal abstract class Extractor<T>
{
    public IEnumerable<T> Extract(IEnumerable<IScriptModel> scripts, string defaultSchemaName)
        => scripts.SelectMany(script => Extract(script, defaultSchemaName));

    public IEnumerable<T> Extract(IEnumerable<TSqlScript> scripts, string defaultSchemaName)
        => scripts.SelectMany(script => Extract(script, defaultSchemaName));

    public IReadOnlyList<T> Extract(IScriptModel script, string defaultSchemaName)
        => ExtractCore(script.ParsedScript, defaultSchemaName);

    public IReadOnlyList<T> Extract(TSqlScript script, string defaultSchemaName)
        => ExtractCore(script, defaultSchemaName);

    protected abstract List<T> ExtractCore(TSqlScript script, string defaultSchemaName);

    protected static InvalidOperationException CreateUnableToDetermineTheDatabaseNameException(string objectType, string objectName, CodeRegion codeRegion)
        => new($"Unable to determine the database name for {objectType} '{objectName}' because neither the object creation statement nor the script contains a preceding 'USE <db-name>' statement. Location: {codeRegion}");
}
