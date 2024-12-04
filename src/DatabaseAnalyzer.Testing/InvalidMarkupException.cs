namespace DatabaseAnalyzer.Testing;

public sealed class InvalidMarkupException : Exception
{
    public InvalidMarkupException(string reason) : base($"The markup is invalid: {reason}")
    {
    }
}
