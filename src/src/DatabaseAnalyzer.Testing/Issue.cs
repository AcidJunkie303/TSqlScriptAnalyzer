using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing;

public sealed record Issue(
    IDiagnosticDefinition DiagnosticDefinition,
    string FullFilePath,
    string? ObjectName,
    ILocation Location,
    IReadOnlyList<string> MessageInsertionStrings
) : IIssue
{
    public static Issue Create(IDiagnosticDefinition diagnosticDefinition, string fullFilePath, string objectName, ILocation location, params IReadOnlyList<string> messageInsertionStrings)
    {
        var expectedInsertionStringCount = InsertionStringHelpers.CountInsertionStrings(diagnosticDefinition.MessageTemplate);
        if (expectedInsertionStringCount != messageInsertionStrings.Count)
        {
            throw new ArgumentException($"Expected {expectedInsertionStringCount} insertion strings, but got {messageInsertionStrings.Count}.", nameof(messageInsertionStrings));
        }

        return new Issue(diagnosticDefinition, fullFilePath, objectName, location, messageInsertionStrings);
    }
}
