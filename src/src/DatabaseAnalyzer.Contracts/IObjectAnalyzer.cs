namespace DatabaseAnalyzer.Contracts;

public interface IObjectAnalyzer
{
    IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; }
}
