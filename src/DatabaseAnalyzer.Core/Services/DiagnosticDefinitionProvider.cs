using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Core.Services;

public sealed class DiagnosticDefinitionProvider : IDiagnosticDefinitionProvider
{
    public DiagnosticDefinitionProvider(IReadOnlyDictionary<string, IDiagnosticDefinition> diagnosticDefinitionsById)
    {
        DiagnosticDefinitionsById = diagnosticDefinitionsById;
    }

    public IReadOnlyDictionary<string, IDiagnosticDefinition> DiagnosticDefinitionsById { get; }

    public IDiagnosticDefinition? GetDiagnosticDefinition(string id)
        => DiagnosticDefinitionsById.GetValueOrDefault(id);
}
