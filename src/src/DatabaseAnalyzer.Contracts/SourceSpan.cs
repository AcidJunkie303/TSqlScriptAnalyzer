using System.Runtime.InteropServices;

namespace DatabaseAnalyzer.Contracts;

[StructLayout(LayoutKind.Auto)]
public record struct SourceSpan(
    int StartLineNumber,
    int StartColumnNumber,
    int EndLineNumber,
    int EndColumnNumber
)
{
    public static SourceSpan Create(int startLineNumber, int startColumnNumber, int endLineNumber, int endColumnNumber)
        => new(startLineNumber, startColumnNumber, endLineNumber, endColumnNumber);
}
