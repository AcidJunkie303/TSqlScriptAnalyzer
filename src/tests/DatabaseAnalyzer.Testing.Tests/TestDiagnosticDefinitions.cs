using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing.Tests;

internal static class TestDiagnosticDefinitions
{
    public static IDiagnosticDefinition TestDiagnostic0 { get; } = new InternalDiagnosticDefinition("TE0000", "Bla", IssueType.Error);
    public static IDiagnosticDefinition TestDiagnostic1 { get; } = new InternalDiagnosticDefinition("TE0001", "Bla {0}", IssueType.Error);
    public static IDiagnosticDefinition TestDiagnostic2 { get; } = new InternalDiagnosticDefinition("TE0002", "Bla {0} {1}", IssueType.Error);

    private sealed class InternalDiagnosticDefinition : IDiagnosticDefinition, IEquatable<InternalDiagnosticDefinition>
    {
        public int RequiredInsertionStringCount { get; }
        public string DiagnosticId { get; }
        public string MessageTemplate { get; }
        public IssueType IssueType { get; }

        public InternalDiagnosticDefinition(string diagnosticId, string messageTemplate, IssueType issueType)
        {
            DiagnosticId = diagnosticId;
            MessageTemplate = messageTemplate;
            IssueType = issueType;
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
