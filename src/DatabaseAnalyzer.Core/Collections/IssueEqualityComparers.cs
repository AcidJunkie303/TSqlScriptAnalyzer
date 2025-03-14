using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Core.Collections;

public static class IssueEqualityComparers
{
    public static IEqualityComparer<IIssue> ByPathAndDatabaseNameAndObjectNameAndCodeRegionAndMessage { get; } = new ByPathAndDatabaseNameAndObjectNameAndCodeRegionAndMessageComparer();

    private sealed class ByPathAndDatabaseNameAndObjectNameAndCodeRegionAndMessageComparer : IEqualityComparer<IIssue>
    {
        public bool Equals(IIssue? x, IIssue? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.DiagnosticDefinition.DiagnosticId.EqualsOrdinalIgnoreCase(y.DiagnosticDefinition.DiagnosticId)
                   && x.RelativeScriptFilePath.EqualsOrdinal(y.RelativeScriptFilePath)
                   && x.DatabaseName.EqualsOrdinal(y.DatabaseName)
                   && x.ObjectName.EqualsOrdinal(y.ObjectName)
                   && x.CodeRegion.Equals(y.CodeRegion)
                   && x.Message.EqualsOrdinal(y.Message);
        }

        public int GetHashCode(IIssue obj)
            => HashCode.Combine(
                obj.DiagnosticDefinition.DiagnosticId.GetHashCode(StringComparison.OrdinalIgnoreCase),
                obj.RelativeScriptFilePath,
                obj.DatabaseName,
                obj.ObjectName,
                obj.CodeRegion,
                obj.Message);
    }
}
