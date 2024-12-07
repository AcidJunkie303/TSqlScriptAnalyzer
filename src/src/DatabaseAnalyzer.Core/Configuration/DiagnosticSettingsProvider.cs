using DatabaseAnalyzer.Contracts;
using Microsoft.Extensions.Configuration;

namespace DatabaseAnalyzer.Core.Configuration;

internal sealed class CachedDiagnosticSettingsProvider : IDiagnosticSettingsProvider
{
    private readonly IDiagnosticSettingsProvider _provider;
    private object? _cachedData;
    private bool _isDataCached;

    public string DiagnosticId => _provider.DiagnosticId;

    public CachedDiagnosticSettingsProvider(IDiagnosticSettingsProvider provider)
    {
        _provider = provider;
    }

    public object? GetSettings(IConfigurationSection configurationSection)
    {
        if (_isDataCached)
        {
            return _cachedData;
        }

        _cachedData = _provider.GetSettings(configurationSection);
        _isDataCached = true;

        return _cachedData;
    }
}
