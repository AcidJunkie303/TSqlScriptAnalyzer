using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing;

internal sealed class FakeDiagnosticSettingsProvider : IDiagnosticSettingsProvider
{
    private readonly IReadOnlyDictionary<string, object?> _settingsByDiagnosticId;

    public FakeDiagnosticSettingsProvider(IReadOnlyDictionary<string, object?> settingsByDiagnosticId)
    {
        _settingsByDiagnosticId = settingsByDiagnosticId;
    }

    public TSettings GetSettings<TSettings>()
        where TSettings : class, ISettings<TSettings>
    {
        if (!_settingsByDiagnosticId.TryGetValue(TSettings.DiagnosticId, out var settings) || settings is null)
        {
            throw new InvalidOperationException($"The settings provider for diagnostic '{TSettings.DiagnosticId}' returned null!");
        }

        return (TSettings) settings;
    }
}
