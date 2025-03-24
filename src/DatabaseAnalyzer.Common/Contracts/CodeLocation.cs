using System.Runtime.InteropServices;

namespace DatabaseAnalyzer.Common.Contracts;

[StructLayout(LayoutKind.Auto)]
public record struct CodeLocation(int Line, int Column) : IComparable<CodeLocation>, IComparable
{
    public static CodeLocation Create(int line, int column) => new(line, column);

    public readonly int CompareTo(object? obj)
        => obj is CodeLocation other
            ? CompareTo(other)
            : throw new ArgumentException($"Is not a {nameof(CodeLocation)} object", nameof(obj));

    public readonly int CompareTo(CodeLocation other)
    {
        var result = Line.CompareTo(other.Line);
        return result == 0
            ? Column.CompareTo(other.Column)
            : result;
    }

    public static bool operator <(CodeLocation left, CodeLocation right) => left.CompareTo(right) < 0;
    public static bool operator >(CodeLocation left, CodeLocation right) => left.CompareTo(right) > 0;
    public static bool operator <=(CodeLocation left, CodeLocation right) => left.CompareTo(right) <= 0;
    public static bool operator >=(CodeLocation left, CodeLocation right) => left.CompareTo(right) >= 0;

    public override readonly string ToString() => $"({Line},{Column})";

    public override readonly int GetHashCode() => HashCode.Combine(Line, Column);

    public readonly bool Equals(CodeLocation other) => Line == other.Line && Column == other.Column;
}
