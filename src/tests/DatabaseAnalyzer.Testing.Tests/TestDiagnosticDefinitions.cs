using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing.Tests;

internal static class TestDiagnosticDefinitions
{
    public static IDiagnosticDefinition TestDiagnostic0 { get; } = new DiagnosticDefinition("TE0000", IssueType.Info, "error 0", "Bla");
    public static IDiagnosticDefinition TestDiagnostic1 { get; } = new DiagnosticDefinition("TE0001", IssueType.Warning, "error 1", "Bla {0}");
    public static IDiagnosticDefinition TestDiagnostic2 { get; } = new DiagnosticDefinition("TE0002", IssueType.Error, "error 2", "Bla {0} {1}");
}
