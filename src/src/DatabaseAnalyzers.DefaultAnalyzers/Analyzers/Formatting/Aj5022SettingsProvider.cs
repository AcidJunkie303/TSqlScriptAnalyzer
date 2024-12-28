using DatabaseAnalyzer.Contracts;
using Microsoft.Extensions.Configuration;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class Aj5022SettingsProvider : IDiagnosticSettingsProvider
{
    public string DiagnosticId => "AJ5022";

    public object GetSettings(IConfigurationSection configurationSection)
        => configurationSection.Get<Aj5022SettingsRaw>()?.ToSettings() ?? Aj5022Settings.Default;
}
