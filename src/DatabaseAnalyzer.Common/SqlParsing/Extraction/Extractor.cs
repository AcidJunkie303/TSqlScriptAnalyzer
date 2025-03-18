using DatabaseAnalyzer.Common.Contracts;

namespace DatabaseAnalyzer.Common.SqlParsing.Extraction;

public abstract class Extractor<T>
{
    protected string DefaultSchemaName { get; }

    protected Extractor(string defaultSchemaName)
    {
        DefaultSchemaName = defaultSchemaName;
    }

    public IEnumerable<T> Extract(IEnumerable<IScriptModel> scripts)
        => scripts.SelectMany(Extract);

    public IReadOnlyList<T> Extract(IScriptModel script)
    {
        ArgumentNullException.ThrowIfNull(script);

        return ExtractCore(script);
    }

    protected abstract IReadOnlyList<T> ExtractCore(IScriptModel script);

    protected static InvalidOperationException CreateUnableToDetermineTheDatabaseNameException(string objectType, string objectName, CodeRegion codeRegion)
        => new($"Unable to determine the database name for {objectType} '{objectName}' because the script contains no preceding 'USE <db-name>' statement. Location: {codeRegion}");
}
