using DatabaseAnalyzer.Contracts;
using Microsoft.Extensions.Configuration;

namespace DatabaseAnalyzer.Core.Configuration;

public static class DiagnosticsSettingsLoader
{
    public static IReadOnlyDictionary<string, object> Load(IEnumerable<SettingsPairTypes> settingsPairTypes, IConfigurationSection diagnosticsConfigurationSection)
    {
        var diagnosticSettingsById = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var (rawType, finalType) in settingsPairTypes)
        {
            var diagnosticId = (string?)finalType.GetProperty(nameof(ISettings.DiagnosticId))?.GetValue(null);
            if (diagnosticId is null)
            {
                // TODO: maybe we should throw an exception here
                continue;
            }

            dynamic raw = diagnosticsConfigurationSection.GetSection(diagnosticId).Get(rawType) ?? Activator.CreateInstance(rawType)!;
            var settings = raw.ToSettings();

            diagnosticSettingsById.Add(diagnosticId, settings);
        }

        return diagnosticSettingsById;
    }
}
