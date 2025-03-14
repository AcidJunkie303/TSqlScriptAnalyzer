using System.Diagnostics.CodeAnalysis;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("maintainability", "CA1515:Consider making public types internal", Justification = "False positive. It is used in the DatabaseAnalyzers.DefaultAnalyzers.Tests project")]
public sealed class InvalidMarkupException : Exception
{
    public InvalidMarkupException(string reason) : base($"The markup is invalid: {reason}")
    {
    }
}
