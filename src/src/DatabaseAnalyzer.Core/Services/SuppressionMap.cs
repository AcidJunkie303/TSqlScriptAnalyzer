using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Core.Extensions;

namespace DatabaseAnalyzer.Core.Services;

internal sealed class SuppressionMap
{
    private readonly IReadOnlyList<SuppressionEntry> _suppressions;

    public SuppressionMap(IReadOnlyList<DiagnosticSuppression> suppressions)
    {
        _suppressions = CreateSuppressionMap(suppressions);
    }

    public IReadOnlyList<DiagnosticSuppression> GetActiveSuppressionsAtLocation(int lineNumber, int columnNumber)
    {
        return _suppressions
            .FirstOrDefault(a => (a.LineNumber == lineNumber && a.ColumnNumber <= columnNumber) || a.LineNumber < lineNumber )
            ?.DisabledDiagnostics ?? [];
    }

    private static List<SuppressionEntry> CreateSuppressionMap(IEnumerable<DiagnosticSuppression> suppressions)
    {
        var sortedSuppressions = suppressions
            .GroupBy(a => a.Location)
            .OrderBy(a => a.Key.LineNumber)
            .ThenBy(a => a.Key.ColumnNumber)
            .Select(a => (Coords: (a.Key.LineNumber, a.Key.ColumnNumber), Suppressions: a.ToList()));

        var activeSuppressions = new LinkedList<DiagnosticSuppression>();
        var map = new List<SuppressionEntry>();

        foreach (var (coords, declaredSuppressions) in sortedSuppressions)
        {
            foreach (var suppression in declaredSuppressions)
            {
                switch (suppression.Action)
                {
                    case SuppressionAction.Disable:
                        activeSuppressions.AddLast(suppression);
                        break;

                    case SuppressionAction.Restore:
                        RemoveLastSuppression(suppression.DiagnosticId);
                        break;

                    case SuppressionAction.Unknown:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException($"{suppression.Action} is not handled");
                }

                AddMapEntry(coords.LineNumber, coords.ColumnNumber);
            }
        }

        return map;

        void AddMapEntry(int lineNumber, int columnNumber)
        {
            var entry = new SuppressionEntry(lineNumber, columnNumber, activeSuppressions.ToList());
            map.Add(entry);
        }

        void RemoveLastSuppression(string diagnosticId)
        {
            activeSuppressions.RemoveLast(a => a.DiagnosticId.EqualsOrdinalIgnoreCase(diagnosticId));
        }
    }

    private sealed record SuppressionEntry(int LineNumber, int ColumnNumber, List<DiagnosticSuppression> DisabledDiagnostics);
}
