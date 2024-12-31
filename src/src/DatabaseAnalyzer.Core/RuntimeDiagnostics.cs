using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Core;

public static class RuntimeDiagnostics
{
    public static DiagnosticDefinition UnhandledError { get; } = new
    (
        "AJ9999",
        IssueType.Warning,
        "General Error",
        "An unhandled error occurred: {0}"
    );
}
