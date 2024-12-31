using System.Collections.Immutable;
using DatabaseAnalyzer.Core.Configuration;

namespace DatabaseAnalyzer.Core.Plugins;

internal sealed record PluginAssembly(
    PluginLoadContext AssemblyLoadContext,
    ImmutableArray<Type> ScriptAnalyzerTypes,
    ImmutableArray<Type> GlobalAnalyzerTypes,
    ImmutableArray<Type> DiagnosticSettingsProviderTypes,
    ImmutableArray<SettingsPairTypes> SettingsTypes
);
