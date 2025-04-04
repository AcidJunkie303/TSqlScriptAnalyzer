namespace DatabaseAnalyzer.Common.Contracts;

public interface IDiagnosticDefinitionProvider
{
    IReadOnlyDictionary<string, IDiagnosticDefinition> DiagnosticDefinitionsById { get; }
    IDiagnosticDefinition? GetDiagnosticDefinition(string id);
}
