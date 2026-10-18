using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Core.Extensions;

namespace DatabaseAnalyzer.Core.Services;

internal sealed class SuppressionMap
{
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "No, we want to bu immutable")]
    private readonly IReadOnlyList<SuppressionEntry> _suppressions;

    public SuppressionMap(IReadOnlyList<DiagnosticSuppression> suppressions)
    {
        _suppressions = CreateSuppressionMap(suppressions);
    }

    public IReadOnlyList<DiagnosticSuppression> GetActiveSuppressionsAtLocation(int lineNumber, int columnNumber)
    {
        // get the currently active suppression at the given line and column number
        for (var i = 0; i < _suppressions.Count; i++)
        {
            var entry = _suppressions[i];
            if (entry.LineNumber > lineNumber || (entry.LineNumber == lineNumber && entry.ColumnNumber > columnNumber))
            {
                return i == 0 ? [] : _suppressions[i - 1].DisabledDiagnostics;
            }
        }

        return [];
    }

    private static List<SuppressionEntry> CreateSuppressionMap(IEnumerable<DiagnosticSuppression> suppressions)
    {
        var sortedSuppressions = suppressions
            .GroupBy(a => a.Location)
            .OrderBy(a => a.Key.Line)
            .ThenBy(a => a.Key.Column)
            .Select(a => (Coords: (LineNumber: a.Key.Line, ColumnNumber: a.Key.Column), Suppressions: a.ToList()));

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

        void RemoveLastSuppression(string diagnosticId) => activeSuppressions.RemoveLast(a => a.DiagnosticId.EqualsOrdinalIgnoreCase(diagnosticId));
    }

    private sealed record SuppressionEntry(int LineNumber, int ColumnNumber, List<DiagnosticSuppression> DisabledDiagnostics);
}
