namespace DatabaseAnalyzer.Contracts;

public interface IIssue
{
    IDiagnosticDefinition DiagnosticDefinition { get; }
    IReadOnlyList<string> MessageInsertionStrings { get; }
    IScript Script { get; }
    ILocation Location { get; }
}
