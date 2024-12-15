using System.Runtime.InteropServices;

namespace DatabaseAnalyzer.Contracts;

[StructLayout(LayoutKind.Auto)]
public record struct CodeRegion(
    int StartLineNumber,
    int StartColumnNumber,
    int EndLineNumber,
    int EndColumnNumber
)
{
    public static CodeRegion Create(CodeLocation startLocation, CodeLocation endLocation)
        => new(startLocation.LineNumber, startLocation.ColumnNumber, endLocation.LineNumber, endLocation.ColumnNumber);

    public static CodeRegion Create(int startLineNumber, int startColumnNumber, int endLineNumber, int endColumnNumber)
        => new(startLineNumber, startColumnNumber, endLineNumber, endColumnNumber);

    public override string ToString() => $"({StartLineNumber},{StartColumnNumber})-({EndLineNumber},{EndColumnNumber})";
}
