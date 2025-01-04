namespace DatabaseAnalyzer.Contracts;

public sealed class DiagnosticDefinition : IDiagnosticDefinition
{
    public DiagnosticDefinition(string diagnosticId, IssueType issueType, string title, string messageTemplate, Uri helpUrl)
    {
        DiagnosticId = diagnosticId;
        IssueType = issueType;
        Title = title;
        MessageTemplate = messageTemplate;
        HelpUrl = new Uri(helpUrl.ToString().Replace("{DiagnosticId}", diagnosticId, StringComparison.OrdinalIgnoreCase));
        RequiredInsertionStringCount = InsertionStringHelpers.CountInsertionStringPlaceholders(messageTemplate);
    }

    public string DiagnosticId { get; }
    public IssueType IssueType { get; }
    public string Title { get; }
    public string MessageTemplate { get; }
    public Uri HelpUrl { get; }
    public int RequiredInsertionStringCount { get; }

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

        return RequiredInsertionStringCount == other.RequiredInsertionStringCount
               && IssueType == other.IssueType
               && string.Equals(DiagnosticId, other.DiagnosticId, StringComparison.Ordinal)
               && string.Equals(MessageTemplate, other.MessageTemplate, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || (obj is IDiagnosticDefinition other2 && Equals(other2));

    public override int GetHashCode()
        => HashCode.Combine(RequiredInsertionStringCount, DiagnosticId, MessageTemplate, (int) IssueType);
}
