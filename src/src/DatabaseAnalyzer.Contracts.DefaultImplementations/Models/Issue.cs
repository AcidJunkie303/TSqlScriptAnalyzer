using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Services;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

public sealed class Issue : IIssue
{
    private Issue(
        IDiagnosticDefinition diagnosticDefinition,
        string relativeScriptFilePath,
        string? objectName,
        CodeRegion codeRegion,
        IReadOnlyList<string> messageInsertionStrings,
        string message)
    {
        DiagnosticDefinition = diagnosticDefinition;
        RelativeScriptFilePath = relativeScriptFilePath;
        ObjectName = objectName;
        CodeRegion = codeRegion;
        MessageInsertionStrings = messageInsertionStrings;
        Message = message;
    }

    public IDiagnosticDefinition DiagnosticDefinition { get; }
    public string Message { get; }
    public string RelativeScriptFilePath { get; }
    public string? ObjectName { get; }
    public CodeRegion CodeRegion { get; }
    public IReadOnlyList<string> MessageInsertionStrings { get; }

    public static Issue Create(IDiagnosticDefinition diagnosticDefinition, string fullFilePath, string? fullObjectName, CodeRegion codeRegion, params IReadOnlyList<object> insertions)
    {
        var expectedInsertionCount = InsertionStringHelpers.CountInsertionStrings(diagnosticDefinition.MessageTemplate);
        if (expectedInsertionCount != insertions.Count)
        {
            throw new ArgumentException($"Expected {expectedInsertionCount} insertions, but got {insertions.Count}.", nameof(insertions));
        }

        var messageInsertionStrings = ToStringArray(insertions);
        var message = InsertionStringHelpers.FormatMessage(diagnosticDefinition.MessageTemplate, messageInsertionStrings);
        return new Issue(diagnosticDefinition, fullFilePath, fullObjectName, codeRegion, messageInsertionStrings, message);
    }

    private static ImmutableArray<string> ToStringArray(IReadOnlyCollection<object> insertionStrings)
        => insertionStrings
            .Select(a => a.ToString() ?? string.Empty)
            .ToImmutableArray();
}
