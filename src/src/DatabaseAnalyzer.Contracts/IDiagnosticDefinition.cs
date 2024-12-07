namespace DatabaseAnalyzer.Contracts;

public interface IDiagnosticDefinition
{
    string DiagnosticId { get; }
    IssueType IssueType { get; }
    string Title { get; }
    string MessageTemplate { get; }
    int RequiredInsertionStringCount { get; }
}

public sealed record DiagnosticDefinition(
    string DiagnosticId,
    IssueType IssueType,
    string Title,
    string MessageTemplate,
    int RequiredInsertionStringCount
) : IDiagnosticDefinition;
