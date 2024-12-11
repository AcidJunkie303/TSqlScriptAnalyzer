using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing.Models;

internal sealed record Issue(
    IDiagnosticDefinition DiagnosticDefinition,
    string FullScriptFilePath,
    string? ObjectName,
    CodeRegion CodeRegion,
    IReadOnlyList<string> MessageInsertionStrings
) : IIssue
{
    public static Issue Create(IDiagnosticDefinition diagnosticDefinition, string fullFilePath, string? fullObjectName, CodeRegion codeRegion, params IReadOnlyList<object> messageInsertionStrings)
        => new(diagnosticDefinition, fullFilePath, fullObjectName, codeRegion, ToStringArray(messageInsertionStrings));

    private static ImmutableArray<string> ToStringArray(IReadOnlyCollection<object> insertionStrings)
        => [.. insertionStrings.Select(a => a.ToString() ?? string.Empty)];
}
