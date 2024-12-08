using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing.Models;

internal sealed record Issue(
    IDiagnosticDefinition DiagnosticDefinition,
    string FullFilePath,
    string? ObjectName,
    SourceSpan CodeRegion,
    IReadOnlyList<string> MessageInsertionStrings
) : IIssue
{
    public static Issue Create(IDiagnosticDefinition diagnosticDefinition, string fullFilePath, string? fullObjectName, SourceSpan codeRegion, params IReadOnlyList<string> messageInsertionStrings) => new(diagnosticDefinition, fullFilePath, fullObjectName, codeRegion, messageInsertionStrings);
}
