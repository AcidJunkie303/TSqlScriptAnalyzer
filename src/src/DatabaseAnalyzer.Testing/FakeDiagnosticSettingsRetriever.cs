using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing;

internal sealed class FakeDiagnosticSettingsRetriever : IDiagnosticSettingsRetriever
{
    private readonly IReadOnlyDictionary<string, object?> _settingsByDiagnosticId;

    public FakeDiagnosticSettingsRetriever(IReadOnlyDictionary<string, object?> settingsByDiagnosticId)
    {
        _settingsByDiagnosticId = settingsByDiagnosticId;
    }

    public T? GetSettings<T>(string diagnosticId)
        where T : class
    {
        if (!_settingsByDiagnosticId.TryGetValue(diagnosticId, out var settings))
        {
            return null;
        }

        return (T?)settings;
    }
}
