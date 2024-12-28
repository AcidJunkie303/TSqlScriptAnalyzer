using DatabaseAnalyzer.Contracts;
using Microsoft.Extensions.Configuration;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class Aj5023SettingsProvider : IDiagnosticSettingsProvider
{
    public string DiagnosticId => "AJ5023";

    public object GetSettings(IConfigurationSection configurationSection)
        => configurationSection.Get<Aj5023SettingsRaw>()?.ToSettings() ?? Aj5023Settings.Default;
}
