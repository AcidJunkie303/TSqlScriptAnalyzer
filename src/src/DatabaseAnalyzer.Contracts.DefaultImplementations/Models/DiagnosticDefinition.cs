using DatabaseAnalyzer.Contracts.DefaultImplementations.Services;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

public sealed class DiagnosticDefinition : IDiagnosticDefinition, IEquatable<IDiagnosticDefinition>, IEquatable<DiagnosticDefinition>
{
    public DiagnosticDefinition(string diagnosticId, IssueType issueType, string title, string messageTemplate)
    {
        DiagnosticId = diagnosticId;
        IssueType = issueType;
        Title = title;
        MessageTemplate = messageTemplate;
        RequiredInsertionStringCount = InsertionStringHelpers.CountInsertionStringPlaceholders(messageTemplate);
    }

    public string DiagnosticId { get; }
    public IssueType IssueType { get; }
    public string Title { get; }
    public string MessageTemplate { get; }
    public int RequiredInsertionStringCount { get; }

    public bool Equals(DiagnosticDefinition? other) => Equals((IDiagnosticDefinition?)other);

    public bool Equals(IDiagnosticDefinition? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return (RequiredInsertionStringCount == other.RequiredInsertionStringCount)
               && string.Equals(DiagnosticId, other.DiagnosticId, StringComparison.Ordinal)
               && string.Equals(MessageTemplate, other.MessageTemplate, StringComparison.Ordinal)
               && (IssueType == other.IssueType);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj)
               || (obj is DiagnosticDefinition other1 && Equals(other1))
               || (obj is IDiagnosticDefinition other2 && Equals(other2));
    }

    public override int GetHashCode() => HashCode.Combine(RequiredInsertionStringCount, DiagnosticId, MessageTemplate, (int)IssueType);
}
