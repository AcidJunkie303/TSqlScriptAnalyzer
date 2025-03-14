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
            var diagnosticId = (string?) finalType.GetProperty(nameof(ISettings.DiagnosticId))?.GetValue(null);
            if (diagnosticId is null)
            {
                // TODO: maybe we should throw an exception here
                continue;
            }

            dynamic raw = diagnosticsConfigurationSection.GetSection(diagnosticId).Get(rawType) ?? Activator.CreateInstance(rawType)!;

            var accessorType = typeof(SettingsAccessor<,>).MakeGenericType(rawType, finalType);
            var accessor = (ISettingsAccessor) Activator.CreateInstance(accessorType, (object[]) [raw])!;
            var settings = accessor.GetSettings();

            diagnosticSettingsById.Add(diagnosticId, settings);
        }

        return diagnosticSettingsById;
    }

    private interface ISettingsAccessor
    {
        object GetSettings();
    }

    private sealed class SettingsAccessor<TRaw, TFinal> : ISettingsAccessor
        where TRaw : class, IRawSettings<TFinal>
        where TFinal : class, ISettings<TFinal>
    {
        private readonly TRaw _rawSettings;

#pragma warning disable S1144 // Unused private types or members should be removed -> ise used above through Activator.CreateInstance
        public SettingsAccessor(TRaw rawSettings)
        {
            _rawSettings = rawSettings;
        }
#pragma warning restore S1144

        public object GetSettings() => _rawSettings.ToSettings();
    }
}
