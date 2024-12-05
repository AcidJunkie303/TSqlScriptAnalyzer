using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts;

public interface IScriptModel
{
    string DatabaseName { get; }
    string FullScriptFilePath { get; }
    string Content { get; }
    SqlScript Script { get; }
}
