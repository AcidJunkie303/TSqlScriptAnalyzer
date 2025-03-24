using DatabaseAnalyzer.Common.Contracts;

namespace DatabaseAnalyzer.Core.Configuration;

internal static class DiagnosticsAccessor
{
    public static IReadOnlyList<IDiagnosticDefinition> GetSupportedDiagnosticDefinitions(Type analyzerType)
    {
        var accessorType = typeof(DiagnosticsAccessorInternal<>).MakeGenericType(analyzerType);
        var accessor = (IDiagnosticsAccessor) Activator.CreateInstance(accessorType)!;
        return accessor.GetDiagnosticDefinitions();
    }

    private interface IDiagnosticsAccessor
    {
        IReadOnlyList<IDiagnosticDefinition> GetDiagnosticDefinitions();
    }

    private sealed class DiagnosticsAccessorInternal<TAnalyzer> : IDiagnosticsAccessor
        where TAnalyzer : IObjectAnalyzer
    {
        public IReadOnlyList<IDiagnosticDefinition> GetDiagnosticDefinitions() => TAnalyzer.SupportedDiagnostics;
    }
}
