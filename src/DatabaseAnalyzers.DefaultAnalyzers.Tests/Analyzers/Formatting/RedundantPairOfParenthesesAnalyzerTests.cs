using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

public sealed class RedundantPairOfParenthesesAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<RedundantPairOfParenthesesAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenNoRedundantPairOfParentheses_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1=1)
                            BEGIN
                                PRINT 'Hello'
                            END
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenRedundantPairOfParentheses_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF ▶️AJ5031💛script_0.sql💛💛((1=1))✅((1=1))◀️
                            BEGIN
                                PRINT 'Hello'
                            END
                            """;
        Verify(code);
    }
}
