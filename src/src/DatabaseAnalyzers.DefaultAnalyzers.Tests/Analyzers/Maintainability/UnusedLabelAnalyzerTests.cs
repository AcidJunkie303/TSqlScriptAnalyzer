using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Maintainability;

public sealed class UnusedLabelAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<UnusedLabelAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenLabelIsUsed_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO
                            
                                GOTO MyLabel -- would cause an error at runtime, but perfect for testing
                            GO
                            
                                GOTO MyLabel
                                PRINT 'Hello'
                            MyLabel:
                                PRINT 'Hello'
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenLabelIsNotUsed_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO
                            
                                PRINT 'Hello'
                            ▶️AJ5036💛script_0.sql💛💛MyLabel✅MyLabel:◀️
                                PRINT 'Hello'
                            """;
        Verify(code);
    }
}
