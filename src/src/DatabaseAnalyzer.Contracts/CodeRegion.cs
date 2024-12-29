using System.Runtime.InteropServices;

namespace DatabaseAnalyzer.Contracts;

[StructLayout(LayoutKind.Auto)]
public record struct CodeRegion(
    int StartLineNumber,
    int StartColumnNumber,
    int EndLineNumber,
    int EndColumnNumber
) : IComparable<CodeRegion>, IComparable
{
    public static CodeRegion Create(CodeLocation startLocation, CodeLocation endLocation)
        => new(startLocation.LineNumber, startLocation.ColumnNumber, endLocation.LineNumber, endLocation.ColumnNumber);

    public static CodeRegion Create(int startLineNumber, int startColumnNumber, int endLineNumber, int endColumnNumber)
        => new(startLineNumber, startColumnNumber, endLineNumber, endColumnNumber);

    public static CodeRegion CreateSpan(CodeRegion start, CodeRegion end)
        => Create(start.StartLineNumber, start.StartColumnNumber, end.EndLineNumber, end.EndColumnNumber);

    public readonly bool IsAround(int lineNumber, int columnNumber)
        => lineNumber >= StartLineNumber
           && columnNumber >= StartColumnNumber
           && lineNumber <= EndLineNumber
           && columnNumber <= EndColumnNumber;

    public readonly int CompareTo(CodeRegion other)
    {
        var result = StartLineNumber.CompareTo(other.StartLineNumber);
        if (result != 0)
        {
            return result;
        }

        result = StartColumnNumber.CompareTo(other.StartColumnNumber);
        if (result != 0)
        {
            return result;
        }

        result = EndLineNumber.CompareTo(other.EndLineNumber);
        return result == 0
            ? EndColumnNumber.CompareTo(other.EndColumnNumber)
            : result;
    }

    public int CompareTo(object? obj)
        => obj is CodeRegion other
            ? CompareTo(other)
            : throw new ArgumentException("obj is not a CodeRegion", nameof(obj));

    public static bool operator <(CodeRegion left, CodeRegion right) => left.CompareTo(right) < 0;
    public static bool operator >(CodeRegion left, CodeRegion right) => left.CompareTo(right) > 0;
    public static bool operator <=(CodeRegion left, CodeRegion right) => left.CompareTo(right) <= 0;
    public static bool operator >=(CodeRegion left, CodeRegion right) => left.CompareTo(right) >= 0;

    public override readonly string ToString() => $"({StartLineNumber},{StartColumnNumber})-({EndLineNumber},{EndColumnNumber})";
}
