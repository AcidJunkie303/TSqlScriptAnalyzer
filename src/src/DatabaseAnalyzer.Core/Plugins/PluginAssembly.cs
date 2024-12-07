using System.Collections.Immutable;

namespace DatabaseAnalyzer.Core.Plugins;

internal sealed record PluginAssembly(
    PluginLoadContext AssemblyLoadContext,
    ImmutableArray<Type> ScriptAnalyzerTypes,
    ImmutableArray<Type> GlobalAnalyzerTypes,
    ImmutableArray<Type> DiagnosticSettingsProviderTypes
);
