namespace DatabaseAnalyzer.Contracts;

public interface IAnalysisContext
{
    string DatabaseName { get; }
    IReadOnlyList<IScript> Scripts { get; }
    IReadOnlyDictionary<string, IScript> ScriptsByDatabaseName { get; }
    void ReportIssue(IDiagnosticDefinition rule, IScript script, ILocation location);
}
