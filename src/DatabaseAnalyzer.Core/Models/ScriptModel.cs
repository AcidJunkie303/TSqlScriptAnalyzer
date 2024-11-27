using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Core.Models;

public sealed record ScriptModel(
    string DatabaseName,
    string FullScriptFilePath,
    string Content,
    SqlScript Script
);
