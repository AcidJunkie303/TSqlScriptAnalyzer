using Microsoft.Extensions.Configuration;

namespace DatabaseAnalyzer.Contracts;

public interface IDiagnosticSettingsProvider
{
    string DiagnosticId { get; }
    object? GetSettings(IConfigurationSection configurationSection);
}
