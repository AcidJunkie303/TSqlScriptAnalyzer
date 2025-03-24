using System.Collections.Frozen;
using DatabaseAnalyzer.Common.Contracts;
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

        return SettingsAccessor.GetSettings(rawSettings, metadata.RawSettingsType, metadata.FinalSettingsType);
    }

    private static IConfigurationSection GetSection(IConfiguration configuration, SettingsSourceKind settingsSourceKind)
    {
        var sectionPath = SectionPathsBySourceKind.GetValueOrDefault(settingsSourceKind)
                          ?? throw new ArgumentException($"SourceKind {settingsSourceKind} is not handled", nameof(settingsSourceKind));

        return configuration.GetSection(sectionPath);
    }
}
