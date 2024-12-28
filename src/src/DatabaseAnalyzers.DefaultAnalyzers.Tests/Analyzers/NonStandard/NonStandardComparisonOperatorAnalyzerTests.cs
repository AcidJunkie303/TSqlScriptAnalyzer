using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.NonStandard;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.NonStandard;

public sealed class NonStandardComparisonOperatorAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<NonStandardComparisonOperatorAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenUsingStandardComparisonOperator_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1 <> 2)
                            BEGIN
                                PRINT 'Hello'
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenUsingNonStandardComparisonOperator_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1 █AJ5032░script_0.sql░░!=███!=█ 2)
                            BEGIN
                                PRINT 'Hello'
                            END


                            """;

        Verify(code);
    }
}
