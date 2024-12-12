using DatabaseAnalyzer.Contracts;
using Microsoft.Extensions.Configuration;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;

public sealed class Aj5000SettingsProvider : IDiagnosticSettingsProvider
{
    public string DiagnosticId => "AJ5001";

    public object GetSettings(IConfigurationSection configurationSection)
        => configurationSection.Get<Aj5001SettingsRaw>()?.ToSettings() ?? Aj5001Settings.Default;
}
