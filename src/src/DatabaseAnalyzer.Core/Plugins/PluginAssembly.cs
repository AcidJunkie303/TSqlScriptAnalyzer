using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Core.Configuration;

namespace DatabaseAnalyzer.Core.Plugins;

internal sealed record PluginAssembly(
    PluginLoadContext AssemblyLoadContext,
    ImmutableArray<Type> ScriptAnalyzerTypes,
    ImmutableArray<Type> GlobalAnalyzerTypes,
    ImmutableArray<Type> DiagnosticSettingsProviderTypes,
    ImmutableArray<SettingsPairTypes> SettingsTypes,
    ImmutableArray<IDiagnosticDefinition> DiagnosticDefinitions
);
