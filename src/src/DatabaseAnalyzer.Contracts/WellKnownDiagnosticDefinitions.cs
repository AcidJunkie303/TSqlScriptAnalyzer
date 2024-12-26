namespace DatabaseAnalyzer.Contracts;

public static class WellKnownDiagnosticDefinitions
{
    public static DiagnosticDefinition FirstScriptStatementIsNotUseStatement { get; } = new
    (
        "AJ9000",
        IssueType.Warning,
        "The first statement in a script must be 'USE <DATABASE>'",
        "The first statement in a script must be 'USE {0}'"
    );

    public static DiagnosticDefinition MissingAlias { get; } = new
    (
        "AJ9001",
        IssueType.Warning,
        "Missing table alias",
        "The column expression {0} cannot be resolved when more than one data source (table, view, etc.) is involved in the statement. To solve this issue, make sure that all data sources are using an alias."
    );

    public static DiagnosticDefinition DuplicateObjectCreationStatement { get; } = new
    (
        "AJ9002",
        IssueType.Error,
        "Duplicate object creation statement",
        "The object '{0}' is created more than once. Script files: '{1}'."
    );
}
