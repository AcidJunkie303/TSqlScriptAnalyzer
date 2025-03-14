using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing.Tests;

internal static class TestDiagnosticDefinitions
{
    public static IDiagnosticDefinition TestDiagnostic0 { get; } = new DiagnosticDefinition("TE0000", IssueType.Information, "error 0", "Bla", [], new Uri("https://dummy.com/{DiagnosticId}"));
    public static IDiagnosticDefinition TestDiagnostic1 { get; } = new DiagnosticDefinition("TE0001", IssueType.Warning, "error 1", "Bla {0}", ["Insertion string #1"], new Uri("https://dummy.com/{DiagnosticId}"));
    public static IDiagnosticDefinition TestDiagnostic2 { get; } = new DiagnosticDefinition("TE0002", IssueType.Error, "error 2", "Bla {0} {1}", ["Insertion string #1", "Insertion string #2"], new Uri("https://dummy.com/{DiagnosticId}"));
}
