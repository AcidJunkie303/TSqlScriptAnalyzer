using System.Collections.Frozen;
using DatabaseAnalyzer.Contracts;
using Microsoft.Extensions.Configuration;

namespace DatabaseAnalyzer.Core;

internal sealed class DiagnosticSettingsRetriever : IDiagnosticSettingsRetriever
{
    private readonly FrozenDictionary<string, object?> _settingsByDiagnosticId;

    public DiagnosticSettingsRetriever(IConfiguration configuration, IEnumerable<IDiagnosticSettingsProvider> diagnosticSettingsProviders)
    {
        var diagnosticsSection = configuration.GetSection("Diagnostics:CustomDiagnosticSettings");

        _settingsByDiagnosticId = diagnosticSettingsProviders
            .ToFrozenDictionary(
                provider => provider.DiagnosticId,
                provider =>
                {
                    var section = diagnosticsSection.GetSection(provider.DiagnosticId);
                    return provider.GetSettings(section);
                },
                StringComparer.OrdinalIgnoreCase);
    }

    public TSettings GetSettings<TSettings>()
        where TSettings : class, ISettings<TSettings>
    {
        if (!_settingsByDiagnosticId.TryGetValue(TSettings.DiagnosticId, out var settings) || settings is null)
        {
            throw new InvalidOperationException($"The settings provider for diagnostic '{TSettings.DiagnosticId}' returned null! Looks like the settings have not been registered.");
        }

        return (TSettings)settings;
    }
}
