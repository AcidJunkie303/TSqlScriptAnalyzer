using DatabaseAnalyzer.Common.Contracts;

namespace DatabaseAnalyzer.Core.Services;

public sealed class DiagnosticDefinitionProvider : IDiagnosticDefinitionProvider
{
    public IReadOnlyDictionary<string, IDiagnosticDefinition> DiagnosticDefinitionsById { get; }

    public DiagnosticDefinitionProvider(IReadOnlyDictionary<string, IDiagnosticDefinition> diagnosticDefinitionsById)
    {
        DiagnosticDefinitionsById = diagnosticDefinitionsById;
    }

    public IDiagnosticDefinition? GetDiagnosticDefinition(string id)
        => DiagnosticDefinitionsById.GetValueOrDefault(id);
}
