using System.Collections.Frozen;
using DatabaseAnalyzer.Contracts;
using Microsoft.Extensions.Configuration;

namespace DatabaseAnalyzer.Core.Configuration;

public static class CustomSettingsLoader
{
    private static readonly FrozenDictionary<SettingsSourceKind, string> SectionPathsBySourceKind = new[]
        {
            (SourceKind: SettingsSourceKind.Diagnostics, Name: "Diagnostics"),
            (SourceKind: SettingsSourceKind.Services, Name: "Services")
        }
        .ToFrozenDictionary(a => a.SourceKind, a => a.Name);

    public static object Load(SettingMetadata settingsMetadata, IConfiguration configuration)
        => LoadCore(settingsMetadata, configuration);

    public static IReadOnlyList<object> Load(IEnumerable<SettingMetadata> settingsMetadata, IConfiguration configuration)
        => settingsMetadata
            .Select(a => LoadCore(a, configuration))
            .ToList();

    private static object LoadCore(SettingMetadata metadata, IConfiguration configuration)
    {
        var section = GetSection(configuration, metadata.SourceKind);
        dynamic rawSettings = section.GetSection(metadata.Name).Get(metadata.RawSettingsType) ?? Activator.CreateInstance(metadata.RawSettingsType)!;

        var accessorType = typeof(SettingsAccessor<,>).MakeGenericType(metadata.RawSettingsType, metadata.FinalSettingsType);
        var accessor = (ISettingsAccessor) Activator.CreateInstance(accessorType, (object[]) [rawSettings])!;
        return accessor.GetSettings();
    }

    private static IConfigurationSection GetSection(IConfiguration configuration, SettingsSourceKind settingsSourceKind)
    {
        var sectionPath = SectionPathsBySourceKind.GetValueOrDefault(settingsSourceKind)
                          ?? throw new ArgumentException($"SourceKind {settingsSourceKind} is not handled", nameof(settingsSourceKind));

        return configuration.GetSection(sectionPath);
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
