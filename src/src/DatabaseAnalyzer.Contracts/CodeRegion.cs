using System.Runtime.InteropServices;

namespace DatabaseAnalyzer.Contracts;

[StructLayout(LayoutKind.Auto)]
public record struct CodeRegion(
    CodeLocation Begin,
    CodeLocation End
) : IComparable<CodeRegion>, IComparable
{
    public static CodeRegion Create(CodeLocation beginLocation, CodeLocation endLocation)
        => new(beginLocation, endLocation);

    public static CodeRegion Create(int beginLineNumber, int beginColumnNumber, int endLineNumber, int endColumnNumber)
        => new(new CodeLocation(beginLineNumber, beginColumnNumber), new CodeLocation(endLineNumber, endColumnNumber));

    public static CodeRegion CreateSpan(CodeRegion begin, CodeRegion end) => Create(begin.Begin, end.End);

    public readonly bool IsAround(int lineNumber, int columnNumber)
        => lineNumber >= Begin.Line
           && columnNumber >= Begin.Column
           && lineNumber <= End.Line
           && columnNumber <= End.Column;

    public readonly int CompareTo(CodeRegion other)
    {
        var result = Begin.CompareTo(other.Begin);
        return result == 0
            ? End.CompareTo(other.End)
            : result;
    }

    public readonly int CompareTo(object? obj)
        => obj is CodeRegion other
            ? CompareTo(other)
            : throw new ArgumentException("obj is not a CodeRegion", nameof(obj));

    public static bool operator <(CodeRegion left, CodeRegion right) => left.CompareTo(right) < 0;
    public static bool operator >(CodeRegion left, CodeRegion right) => left.CompareTo(right) > 0;
    public static bool operator <=(CodeRegion left, CodeRegion right) => left.CompareTo(right) <= 0;
    public static bool operator >=(CodeRegion left, CodeRegion right) => left.CompareTo(right) >= 0;

    public override readonly string ToString() => $"{Begin} - {End}";
}
