namespace DatabaseAnalyzer.Contracts;

public interface IAnalysisContext
{
    string DatabaseName { get; }
    IReadOnlyList<IScriptModel> Scripts { get; }
    IReadOnlyDictionary<string, IScriptModel> ScriptsByDatabaseName { get; }
    IDiagnosticSettingsProviderFactory DiagnosticSettingsProviderFactory { get; }
    void ReportIssue(IDiagnosticDefinition rule, string fullScriptFilePath, string? fullObjectName, SourceSpan codeRegion, params IReadOnlyList<string> insertionStrings);
}
