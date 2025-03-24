using DatabaseAnalyzer.Common.Contracts;

namespace DatabaseAnalyzer.Core.Plugins;

internal sealed record AnalyzerTypeAndDiagnostics(
    Type Type,
    IReadOnlyList<IDiagnosticDefinition> Diagnostics
);
