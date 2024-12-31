using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;

public interface IDatabaseObjectExtractor
{
    IReadOnlyDictionary<string, DatabaseInformation> Extract(IReadOnlyCollection<IScriptModel> scripts, string defaultSchemaName);
}
