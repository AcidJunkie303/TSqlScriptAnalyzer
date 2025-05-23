using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;
using Xunit.Abstractions;

#pragma warning disable

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Runtime;

public sealed class SelectStarAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<SelectStarAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenNoSelectStar_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      Value
                            FROM        Table1
                            WHERE       Id = 1
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenSelectStarFromTable_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      ▶️AJ5041💛script_0.sql💛✅*◀️
                            FROM        Table1
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenSelectStarWithAliasFromTable_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      ▶️AJ5041💛script_0.sql💛✅t1.*◀️
                            FROM        Table1 t1
                            """;
        Verify(code);
    }

    [Fact]
    public void WithJoin_WhenSelectStarRefersToDerivedJoinTable_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      t1.Id,
                                        d.*
                            FROM        Table1 AS t1
                            INNER JOIN
                            (
                                SELECT  Id,
                                        Value
                                FROM    Table2
                            ) AS d ON d.Id = t1.Id
                            """;
        Verify(code);
    }

    [Fact]
    public void WithJoin_WhenSelectStarRefersToNonDerivedJoinTable_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      t1.Id,
                                        ▶️AJ5041💛script_0.sql💛✅t2.*◀️
                            FROM        Table1 AS t1
                            INNER JOIN  Table2 AS t2 ON t2.Id = t1.Id
                            """;
        Verify(code);
    }

    [Theory]
    [InlineData("/* 0001 */ 1")]
    [InlineData("/* 0002 */ ▶️AJ5053💛script_0.sql💛✅*◀️")]
    public void WhenExistsCheckInSubqueryWithSelectStar_ThenDiagnose(string insertion)
    {
        var code = $"""
                    USE MyDb
                    GO

                    SELECT      t1.Value
                    FROM        Table1 t1
                    WHERE EXISTS (
                        SELECT  {insertion}
                        FROM    Table2 t2
                        WHERE   t2.Id = t1.Id
                    );

                    """;
        Verify(code);
    }

    [Theory]
    [InlineData("/* 0001 */ 1")]
    [InlineData("/* 0002 */ ▶️AJ5053💛script_0.sql💛✅*◀️")]
    public void WhenExistsCheckWithSelectStar_ThenDiagnose(string insertion)
    {
        var code = $"""
                    USE MyDb
                    GO

                    IF EXISTS (SELECT {insertion} FROM T1 WHERE Value = 'value')
                    BEGIN
                        PRINT 'Hello'
                    END

                    """;
        Verify(code);
    }
}
