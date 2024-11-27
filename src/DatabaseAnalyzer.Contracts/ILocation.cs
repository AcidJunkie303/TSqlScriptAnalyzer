namespace DatabaseAnalyzer.Contracts;

public interface ILocation
{
    int StartLineNumber { get; }
    int EndLineNumber { get; }
    int StartColumnNumber { get; }
    int EndColumnNumber { get; }
}
