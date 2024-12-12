using DatabaseAnalyzer.Contracts;
using Microsoft.Extensions.Configuration;

namespace DatabaseAnalyzers.DefaultAnalyzers.Configuration;

public abstract class DiagnosticSettingsProviderBase<TRawSettings, TSettings> : IDiagnosticSettingsProvider
    where TRawSettings : class, IRawSettings<TSettings>, new()
    where TSettings : class, ISettings<TSettings>
{
    public abstract string DiagnosticId { get; }

    public object GetSettings(IConfigurationSection configurationSection)
        => configurationSection.Get<TRawSettings>()?.ToSettings() ?? TSettings.Default;
}
