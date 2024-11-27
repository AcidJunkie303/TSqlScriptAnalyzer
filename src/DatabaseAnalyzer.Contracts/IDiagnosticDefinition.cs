namespace DatabaseAnalyzer.Contracts;

public interface IDiagnosticDefinition
{
    string DiagnosticId { get; }
    string MessageTemplate { get; }
}
