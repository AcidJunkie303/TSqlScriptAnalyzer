using System.Runtime.InteropServices;

namespace DatabaseAnalyzer.Contracts;

public interface ILocation
{
    int StartLineNumber { get; }
    int StartColumnNumber { get; }
    int EndLineNumber { get; }
    int EndColumnNumber { get; }
}

[StructLayout(LayoutKind.Auto)]
public record struct Location(
    int StartLineNumber,
    int StartColumnNumber,
    int EndLineNumber,
    int EndColumnNumber
) : ILocation
{
    public static Location Create(int startLineNumber, int startColumnNumber, int endLineNumber, int endColumnNumber)
        => new(startLineNumber, startColumnNumber, endLineNumber, endColumnNumber);
}
