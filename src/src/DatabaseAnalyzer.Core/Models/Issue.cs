using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Core.Models;

internal sealed record Issue(
    IDiagnosticDefinition DiagnosticDefinition,
    string FullFilePath,
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
        => [.. insertionStrings.Select(a => a.ToString() ?? string.Empty)];
}
