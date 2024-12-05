namespace DatabaseAnalyzer.Core.Models;

public sealed record BasicScriptInformation(
    string FullScriptPath,
    string DatabaseName,
    string Content
);
