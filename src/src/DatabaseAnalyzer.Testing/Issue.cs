using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing;

public sealed record Issue(
    IDiagnosticDefinition DiagnosticDefinition,
    string FullFilePath,
    string? ObjectName,
    SourceSpan CodeRegion,
    IReadOnlyList<string> MessageInsertionStrings
) : IIssue
{
    public static Issue Create(IDiagnosticDefinition diagnosticDefinition, string fullFilePath, string objectName, SourceSpan codeRegion, params IReadOnlyList<string> messageInsertionStrings)
    {
        var expectedInsertionStringCount = InsertionStringHelpers.CountInsertionStrings(diagnosticDefinition.MessageTemplate);
        if (expectedInsertionStringCount != messageInsertionStrings.Count)
        {
            throw new ArgumentException($"Expected {expectedInsertionStringCount} insertion strings, but got {messageInsertionStrings.Count}.", nameof(messageInsertionStrings));
        }

        return new Issue(diagnosticDefinition, fullFilePath, objectName, codeRegion, messageInsertionStrings);
    }
}
