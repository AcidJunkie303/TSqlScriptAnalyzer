namespace DatabaseAnalyzer.Common.Contracts;

public interface IDiagnosticDefinitionRegistry
{
    IDiagnosticDefinition GetDefinition(string id);
}
