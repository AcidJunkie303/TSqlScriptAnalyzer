namespace DatabaseAnalyzer.Contracts;

public sealed record Issue(
    IDiagnosticDefinition DiagnosticDefinition,
    string FullFilePath,
    string? ObjectName,
    ILocation Location,
    IReadOnlyList<string> MessageInsertionStrings
) : IIssue
{
    public static Issue Create(IDiagnosticDefinition diagnosticDefinition, string fullFilePath, string objectName, ILocation location, IReadOnlyList<string> messageInsertionStrings)
        => new(diagnosticDefinition, fullFilePath, objectName, location, messageInsertionStrings);
}
