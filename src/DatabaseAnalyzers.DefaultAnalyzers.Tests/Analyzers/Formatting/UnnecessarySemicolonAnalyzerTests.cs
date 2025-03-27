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

                            SELECT      1;

                            WITH CTE AS
                            (
                                SELECT * FROM Table1
                            )
                            SELECT      *
                            FROM        CTE c

                            SELECT      1
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenSemiColonBeforeMergeStatement_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      1

                            MERGE INTO T1 USING T2 ON 1=1
                            WHEN MATCHED THEN UPDATE SET Column1 = Value1;

                            SELECT       1
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenSemiColonBeforeThrowStatement_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      1;

                            THROW 51000, 'The record does not exist.', 1

                            SELECT      1
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenSemiColonNotRequired_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT 'tb'▶️AJ5028💛script_0.sql💛✅;◀️
                            SELECT 303
                            """;
        Verify(code);
    }
}
