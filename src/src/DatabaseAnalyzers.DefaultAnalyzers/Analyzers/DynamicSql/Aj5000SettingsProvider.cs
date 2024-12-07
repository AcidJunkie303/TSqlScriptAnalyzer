using DatabaseAnalyzer.Contracts;
using Microsoft.Extensions.Configuration;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.DynamicSql;

public sealed class Aj5000SettingsProvider : IDiagnosticSettingsProvider
{
    public string DiagnosticId => "AJ5000";

    public object GetSettings(IConfigurationSection configurationSection)
        => configurationSection.Get<Aj5000SettingsRaw>()?.ToSettings() ?? Aj5000Settings.Default;
}
