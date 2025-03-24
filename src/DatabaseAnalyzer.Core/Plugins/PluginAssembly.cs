using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Core.Configuration;

namespace DatabaseAnalyzer.Core.Plugins;

internal sealed record PluginAssembly(
    string AssemblyFilePath,
    PluginLoadContext AssemblyLoadContext,
    ImmutableArray<AnalyzerTypeAndDiagnostics> ScriptAnalyzerTypes,
    ImmutableArray<AnalyzerTypeAndDiagnostics> GlobalAnalyzerTypes,
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
