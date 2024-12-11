using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Services;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

public sealed record Issue(
    IDiagnosticDefinition DiagnosticDefinition,
    string FullScriptFilePath,
    string? ObjectName,
    CodeRegion CodeRegion,
    IReadOnlyList<string> MessageInsertionStrings
) : IIssue
{
    public static Issue Create(IDiagnosticDefinition diagnosticDefinition, string fullFilePath, string? fullObjectName, CodeRegion codeRegion, params IReadOnlyList<object> messageInsertionStrings)
    {
        var expectedInsertionStringCount = InsertionStringHelpers.CountInsertionStrings(diagnosticDefinition.MessageTemplate);
        if (expectedInsertionStringCount != messageInsertionStrings.Count)
        {
            throw new ArgumentException($"Expected {expectedInsertionStringCount} insertion strings, but got {messageInsertionStrings.Count}.", nameof(messageInsertionStrings));
        }

        return new Issue(diagnosticDefinition, fullFilePath, fullObjectName, codeRegion, ToStringArray(messageInsertionStrings));
    }

    private static ImmutableArray<string> ToStringArray(IReadOnlyCollection<object> insertionStrings)
        => insertionStrings
            .Select(a => a.ToString() ?? string.Empty)
            .ToImmutableArray();
}
