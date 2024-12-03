namespace DatabaseAnalyzer.Contracts;

public interface IIssue
{
    IDiagnosticDefinition DiagnosticDefinition { get; }
    IReadOnlyList<string> MessageInsertionStrings { get; }
    IScriptModel Script { get; }
    ILocation Location { get; }
}
