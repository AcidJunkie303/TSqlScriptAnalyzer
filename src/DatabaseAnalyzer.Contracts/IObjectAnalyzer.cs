namespace DatabaseAnalyzer.Contracts;

public interface IObjectAnalyzer
{
    static virtual IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [];
}
