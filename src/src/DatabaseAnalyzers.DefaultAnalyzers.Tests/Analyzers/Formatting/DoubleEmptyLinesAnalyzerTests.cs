using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

public sealed class DoubleEmptyLinesAnalyzerTests(ITestOutputHelper testOutputHelper) : ScriptAnalyzerTestsBase<DoubleEmptyLinesAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenOnlySingleEmptyLine_ThenOk()
    {
        const string code = """
                            PRINT 303

                            PRINT 909
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenDoubleEmptyLines_ThenDiagnose()
    {
        const string code = """
                            PRINT 303█AJ5007░main.sql░███


                            █PRINT 909
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenSingleEmptyLineAtEnd_ThenOk()
    {
        const string code = """

                            PRINT 303

                            """;
        Verify(code);
    }
}
