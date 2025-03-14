using System.Collections.Frozen;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Core.Configuration;

public sealed class DiagnosticSettingsProvider : IDiagnosticSettingsProvider
{
    private readonly FrozenDictionary<string, object> _diagnosticsSettingsById;

    public DiagnosticSettingsProvider(FrozenDictionary<string, object> diagnosticsSettingsById)
    {
        _diagnosticsSettingsById = diagnosticsSettingsById;
    }

    public TSettings GetSettings<TSettings>()
        where TSettings : class, ISettings<TSettings>
    {
        var settings = _diagnosticsSettingsById.GetValueOrDefault(TSettings.DiagnosticId)
                       ?? throw new InvalidOperationException($"No settings registered for diagnostic id '{TSettings.DiagnosticId}'!");

        return (TSettings) settings;
    }
}
