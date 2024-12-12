using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;
using Microsoft.Extensions.Configuration;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UseDatabaseStatements;

public sealed class Aj5003SettingsProvider : IDiagnosticSettingsProvider
{
    public string DiagnosticId => "AJ5001";

    public object GetSettings(IConfigurationSection configurationSection)
        => configurationSection.Get<Aj5001SettingsRaw>()?.ToSettings() ?? Aj5001Settings.Default;
}
