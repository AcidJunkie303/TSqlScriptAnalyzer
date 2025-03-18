namespace DatabaseAnalyzer.Common.Contracts;

public interface IObjectAnalyzer
{
    static virtual IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [];
}
