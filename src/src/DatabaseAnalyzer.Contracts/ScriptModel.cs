using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts;

public sealed record ScriptModel(
    string DatabaseName,
    string FullScriptFilePath,
    string Content,
    SqlScript Script,
    IReadOnlyList<string> Errors,
    IReadOnlyList<Suppression> Suppressions
);
