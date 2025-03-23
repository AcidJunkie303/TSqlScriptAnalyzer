using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Models;

namespace DatabaseAnalyzer.Common.SqlParsing.Extraction;

public interface IDatabaseObjectExtractor
{
    IReadOnlyDictionary<string, DatabaseInformation> Extract(IReadOnlyCollection<IScriptModel> scripts, string defaultSchemaName);
}
