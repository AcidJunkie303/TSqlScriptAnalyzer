namespace DatabaseAnalyzer.Contracts;

public interface IDiagnosticSettingsProviderFactory
{
    IDiagnosticSettingsProvider GetDiagnosticSettingsProvider(string diagnosticId);
}
