using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Core.Configuration;

namespace DatabaseAnalyzer.Core.Plugins;

internal sealed record PluginAssembly(
    PluginLoadContext AssemblyLoadContext,
    ImmutableArray<Type> ScriptAnalyzerTypes,
    ImmutableArray<Type> GlobalAnalyzerTypes,
    ImmutableArray<Type> ServiceTypes,
    ImmutableArray<SettingMetadata> CustomSettings,
    ImmutableArray<IDiagnosticDefinition> DiagnosticDefinitions
) : IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        AssemblyLoadContext.Dispose();

        _disposed = true;
    }
}
