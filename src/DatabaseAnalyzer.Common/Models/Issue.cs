using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Common.Models;

public sealed class Issue : IIssue
{
    private Issue(
        IDiagnosticDefinition diagnosticDefinition,
        string databaseName,
        string relativeScriptFilePath,
        string? objectName,
        CodeRegion codeRegion,
        IReadOnlyList<object> messageInsertionStrings,
        string message)
    {
        DiagnosticDefinition = diagnosticDefinition;
        DatabaseName = databaseName;
        RelativeScriptFilePath = relativeScriptFilePath;
        ObjectName = objectName;
        CodeRegion = codeRegion;
        MessageInsertions = messageInsertionStrings;
        Message = message;
    }

    public string DatabaseName { get; }

    public IDiagnosticDefinition DiagnosticDefinition { get; }
    public string RelativeScriptFilePath { get; }
    public string? ObjectName { get; }
    public CodeRegion CodeRegion { get; }
    public IReadOnlyList<object> MessageInsertions { get; }
    public string Message { get; }

    public static Issue Create(IDiagnosticDefinition diagnosticDefinition, string databaseName, string relativeScriptFilePath, string? fullObjectName, CodeRegion codeRegion, params object[] insertions)
    {
        ArgumentNullException.ThrowIfNull(diagnosticDefinition);
        ArgumentNullException.ThrowIfNull(insertions);

        var expectedInsertionCount = InsertionStringHelpers.CountInsertionStringPlaceholders(diagnosticDefinition.MessageTemplate);
        if (expectedInsertionCount != insertions.Length)
        {
            throw new ArgumentException($"Expected {expectedInsertionCount} insertions, but got {insertions.Length}.", nameof(insertions));
        }

        var messageInsertionStrings = insertions
            .Select(static a => a.ToString() ?? string.Empty)
            .ToList();
        var message = InsertionStringHelpers.FormatMessage(diagnosticDefinition.MessageTemplate, messageInsertionStrings);
        return new Issue(diagnosticDefinition, databaseName, relativeScriptFilePath, fullObjectName, codeRegion, messageInsertionStrings, message);
    }
}
