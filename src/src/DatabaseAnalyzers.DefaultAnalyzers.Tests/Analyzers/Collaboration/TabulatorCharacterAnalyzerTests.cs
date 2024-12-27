using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Collaboration;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Collaboration;

public sealed class TabulatorCharacterAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<TabulatorCharacterAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenNoTabFound_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenTabFound_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1=1)
                            BEGIN
                            █AJ5019░script_0.sql░███	█PRINT 'Hello' -- the line begins with a tab
                            END
                            """;

        Verify(code);
    }
}
