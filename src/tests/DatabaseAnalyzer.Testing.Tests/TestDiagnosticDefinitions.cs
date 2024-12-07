using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing.Tests;

internal static class TestDiagnosticDefinitions
{
    public static IDiagnosticDefinition TestDiagnostic0 { get; } = new InternalDiagnosticDefinition("TE0000", IssueType.Info, "error 0", "Bla");
    public static IDiagnosticDefinition TestDiagnostic1 { get; } = new InternalDiagnosticDefinition("TE0001", IssueType.Warning, "error 1", "Bla {0}");
    public static IDiagnosticDefinition TestDiagnostic2 { get; } = new InternalDiagnosticDefinition("TE0002", IssueType.Error, "error 2", "Bla {0} {1}");

    private sealed class InternalDiagnosticDefinition : IDiagnosticDefinition, IEquatable<InternalDiagnosticDefinition>
    {
        public string DiagnosticId { get; }
        public IssueType IssueType { get; }
        public string Title { get; }
        public string MessageTemplate { get; }
        public int RequiredInsertionStringCount { get; }

        public InternalDiagnosticDefinition(string diagnosticId, IssueType issueType, string title, string messageTemplate)
        {
            DiagnosticId = diagnosticId;
            IssueType = issueType;
            Title = title;
            MessageTemplate = messageTemplate;
            RequiredInsertionStringCount = InsertionStringHelpers.CountInsertionStrings(messageTemplate);
        }

        public bool Equals(InternalDiagnosticDefinition? other)
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
                   && string.Equals(DiagnosticId, other.DiagnosticId, StringComparison.Ordinal)
                   && string.Equals(MessageTemplate, other.MessageTemplate, StringComparison.Ordinal)
                   && IssueType == other.IssueType;
        }

        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is InternalDiagnosticDefinition other && Equals(other));

        public override int GetHashCode() => HashCode.Combine(RequiredInsertionStringCount, DiagnosticId, MessageTemplate, (int)IssueType);
    }
}
