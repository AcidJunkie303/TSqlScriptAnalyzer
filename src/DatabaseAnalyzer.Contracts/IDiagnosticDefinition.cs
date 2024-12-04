namespace DatabaseAnalyzer.Contracts;

public interface IDiagnosticDefinition
{
    string DiagnosticId { get; }
    string MessageTemplate { get; }
    IssueType IssueType { get; }
    int RequiredInsertionStringCount { get; }
}

public sealed record DiagnosticDefinition(
    string DiagnosticId,
    string MessageTemplate,
    IssueType IssueType
) : IDiagnosticDefinition
{
    public int RequiredInsertionStringCount { get; } =
}
