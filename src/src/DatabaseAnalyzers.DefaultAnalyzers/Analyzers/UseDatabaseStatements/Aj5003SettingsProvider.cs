using DatabaseAnalyzer.Contracts;
using Microsoft.Extensions.Configuration;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UseDatabaseStatements;

public sealed class Aj5003SettingsProvider : IDiagnosticSettingsProvider
{
    public string DiagnosticId => "AJ5003";

    public object GetSettings(IConfigurationSection configurationSection)
        => configurationSection.Get<Aj5003SettingsRaw>()?.ToSettings() ?? Aj5003Settings.Default;
}
