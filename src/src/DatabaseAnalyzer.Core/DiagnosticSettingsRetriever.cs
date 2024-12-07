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
