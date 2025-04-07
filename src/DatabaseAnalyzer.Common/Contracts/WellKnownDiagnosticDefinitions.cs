using System.Diagnostics.CodeAnalysis;

namespace DatabaseAnalyzer.Common.Contracts;

[SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded")]
public static class WellKnownDiagnosticDefinitions
{
    public static DiagnosticDefinition FirstScriptStatementIsNotUseStatement { get; } = new
    (
        "AJ9000",
        IssueType.Warning,
        "The first statement in a script must be 'USE <DATABASE>'",
        "The very first statement in a script must be a `USE` statements at location 1,1",
        [],
        new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/AJ9000.md")
    );

    public static DiagnosticDefinition MissingAlias { get; } = new
    (
        "AJ9001",
        IssueType.Warning,
        "Missing table alias",
        "The column expression `{0}` cannot be resolved when more than one data source (table, view, etc.) is involved in the statement. " +
        "To solve this issue, make sure that all columns are referenced using an alias.",
        ["Column reference expression"],
        new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/AJ9001.md")
    );

    public static DiagnosticDefinition DuplicateObjectCreationStatement { get; } = new
    (
        "AJ9002",
        IssueType.Error,
        "Duplicate object creation statement",
        "The object `{0}` is created more than once. Script file(s): `{1}`.",
        ["Object name", "Script file path"],
        new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/AJ9002.md")
    );

    public static DiagnosticDefinition ScriptContainsErrors { get; } = new
    (
        "AJ9004",
        IssueType.Error,
        "Error in script",
        "The script contains one or more errors: {0}.",
        ["Error message"],
        new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/AJ9004.md")
    );

    public static DiagnosticDefinition UnhandledAnalyzerException { get; } = new
    (
        "AJ9999",
        IssueType.Error,
        "Analyzer error",
        "The analyzer `{0}` threw an exception: {1}.",
        ["Analyzer name", "Exception message"],
        new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/AJ9999.md")
    );
}
