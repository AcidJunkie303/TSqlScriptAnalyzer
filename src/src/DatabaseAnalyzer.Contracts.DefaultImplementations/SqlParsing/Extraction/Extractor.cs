using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;

public abstract class Extractor<T>
{
    protected Extractor(string defaultSchemaName)
    {
        DefaultSchemaName = defaultSchemaName;
    }

    protected string DefaultSchemaName { get; }

    public IEnumerable<T> Extract(IEnumerable<IScriptModel> scripts)
        => scripts.SelectMany(a => Extract(a, a.RelativeScriptFilePath));

    public IReadOnlyList<T> Extract(IScriptModel script, string relativeScriptFilePath)
    {
        ArgumentNullException.ThrowIfNull(script);

        return ExtractCore(script.ParsedScript, relativeScriptFilePath);
    }

    public IReadOnlyList<T> Extract(TSqlScript script, string relativeScriptFilePath)
        => ExtractCore(script, relativeScriptFilePath);

    protected abstract IReadOnlyList<T> ExtractCore(TSqlScript script, string relativeScriptFilePath);

    protected static InvalidOperationException CreateUnableToDetermineTheDatabaseNameException(string objectType, string objectName, CodeRegion codeRegion)
        => new($"Unable to determine the database name for {objectType} '{objectName}' because the script contains no preceding 'USE <db-name>' statement. Location: {codeRegion}");
}
