using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers;

internal static class SharedDiagnosticDefinitions
{
    public static DiagnosticDefinition MissingObject { get; } = new
    (
        "AJ5044",
        IssueType.Warning,
        "Missing Object",
        "The referenced `{0}` `{1}` was not found.",
        ["Object Type Name", "Full Object Name"],
        UrlPatterns.DefaultDiagnosticHelp
    );
}
