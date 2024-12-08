using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing;

internal sealed class DiagnosticDefinitionRegistry : IDiagnosticDefinitionRegistry
{
    private readonly Dictionary<string, IDiagnosticDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);

    public DiagnosticDefinitionRegistry(IEnumerable<IDiagnosticDefinition> diagnosticDefinitions)
    {
        foreach (var definition in diagnosticDefinitions)
        {
            Register(definition);
        }
    }

    public void Register(IDiagnosticDefinition definition) => _definitions.Add(definition.DiagnosticId, definition);

    public IDiagnosticDefinition GetDefinition(string id)
        => _definitions.GetValueOrDefault(id) ?? throw new IOException($"The diagnostic definition with id '{id}' is not registered.");
}
