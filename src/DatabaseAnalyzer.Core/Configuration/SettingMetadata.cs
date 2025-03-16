using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Core.Configuration;

public record SettingMetadata(Type RawSettingsType, Type FinalSettingsType, SettingsSourceKind SourceKind, string Name);
