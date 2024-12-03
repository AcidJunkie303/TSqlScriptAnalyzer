namespace DatabaseAnalyzer.Contracts;

public interface IAnalysisContext
{
    string DatabaseName { get; }
    IReadOnlyList<IScriptModel> Scripts { get; }
    IReadOnlyDictionary<string, IScriptModel> ScriptsByDatabaseName { get; }
    void ReportIssue(IDiagnosticDefinition rule, IScriptModel script, ILocation location);
}
