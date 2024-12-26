using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public interface IDatabaseObject
{
    string DatabaseName { get; }
    string ObjectName { get; }
    IReadOnlyList<string> FullNameParts { get; }
    TSqlFragment CreationStatement { get; }
    string RelativeScriptFilePath { get; }
}
