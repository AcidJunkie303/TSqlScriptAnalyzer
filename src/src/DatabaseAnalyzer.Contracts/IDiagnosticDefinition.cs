namespace DatabaseAnalyzer.Contracts;

public interface IDiagnosticDefinition : IEquatable<IDiagnosticDefinition>
{
    string DiagnosticId { get; }
    IssueType IssueType { get; }
    string Title { get; }
    string MessageTemplate { get; }
    IReadOnlyList<string> InsertionStringDescriptions { get; }
    Uri HelpUrl { get; }
}
