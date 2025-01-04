using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

public sealed class UnnecessarySemicolonAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<UnnecessarySemicolonAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenSemiColonBeforeWithStatement_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      1
                            ;
                            WITH CTE AS
                            (
                                SELECT * FROM Table1
                            )
                            SELECT      *
                            FROM        CTE c
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenSemiColonAfterMergeStatement_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            MERGE INTO T1 USING T2 ON 1=1
                            WHEN MATCHED THEN UPDATE SET Column1 = Value1;
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenSemiColonNotRequired_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT 303
                            ▶️AJ5028💛script_0.sql💛✅;◀️
                            SELECT 303
                            """;
        Verify(code);
    }
}
