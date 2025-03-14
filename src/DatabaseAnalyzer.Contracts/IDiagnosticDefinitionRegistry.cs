namespace DatabaseAnalyzer.Contracts;

public interface IDiagnosticDefinitionRegistry
{
    IDiagnosticDefinition GetDefinition(string id);
}
