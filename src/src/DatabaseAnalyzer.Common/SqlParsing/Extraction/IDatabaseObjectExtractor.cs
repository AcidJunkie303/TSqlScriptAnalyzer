using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Common.SqlParsing.Extraction;

public interface IDatabaseObjectExtractor
{
    IReadOnlyDictionary<string, DatabaseInformation> Extract(IReadOnlyCollection<IScriptModel> scripts, string defaultSchemaName);
}
