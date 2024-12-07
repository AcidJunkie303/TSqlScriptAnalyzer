using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Core;

internal sealed record AnalysisContext(
    string DatabaseName,
    IReadOnlyList<IScriptModel> Scripts,
    IReadOnlyDictionary<string, IScriptModel> ScriptsByDatabaseName,
    IDiagnosticSettingsProviderFactory DiagnosticSettingsProviderFactory
) : IAnalysisContext
{
    public void ReportIssue(IDiagnosticDefinition rule, string fullScriptFilePath, string? fullObjectName, SourceSpan codeRegion, params IReadOnlyList<string> insertionStrings) => throw new NotImplementedException();
}
