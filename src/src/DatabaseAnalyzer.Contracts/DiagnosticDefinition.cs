namespace DatabaseAnalyzer.Contracts;

public sealed class DiagnosticDefinition : IDiagnosticDefinition
{
    public DiagnosticDefinition(string diagnosticId, IssueType issueType, string title, string messageTemplate, IReadOnlyList<string> insertionStringDescriptions, Uri helpUrl)
    {
        DiagnosticId = diagnosticId;
        IssueType = issueType;
        Title = title;
        MessageTemplate = messageTemplate;
        InsertionStringDescriptions = insertionStringDescriptions;
        HelpUrl = new Uri(helpUrl.ToString().Replace("{DiagnosticId}", diagnosticId, StringComparison.OrdinalIgnoreCase));
        RequiredInsertionStringCount = InsertionStringHelpers.CountInsertionStringPlaceholders(messageTemplate);
    }

    public int RequiredInsertionStringCount { get; }

    public string DiagnosticId { get; }
    public IssueType IssueType { get; }
    public string Title { get; }
    public string MessageTemplate { get; }
    public IReadOnlyList<string> InsertionStringDescriptions { get; }
    public Uri HelpUrl { get; }

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

        return InsertionStringDescriptions.Count == other.InsertionStringDescriptions.Count
               && IssueType == other.IssueType
               && string.Equals(DiagnosticId, other.DiagnosticId, StringComparison.Ordinal)
               && string.Equals(MessageTemplate, other.MessageTemplate, StringComparison.Ordinal)
               && InsertionStringDescriptions.SequenceEqual(other.InsertionStringDescriptions, StringComparer.Ordinal);
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || (obj is IDiagnosticDefinition other2 && Equals(other2));

    public override int GetHashCode()
        => HashCode.Combine(RequiredInsertionStringCount, DiagnosticId, MessageTemplate, (int) IssueType);
}
