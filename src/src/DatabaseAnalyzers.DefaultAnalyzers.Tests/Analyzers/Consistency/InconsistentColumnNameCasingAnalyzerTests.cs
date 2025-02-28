using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Consistency;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Consistency;

public sealed class InconsistentColumnNameCasingAnalyzerTests(ITestOutputHelper testOutputHelper)
    : GlobalAnalyzerTestsBase<InconsistentColumnNameCasingAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenColumnHasSameCasing_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                Column1     INT
                            )
                            GO

                            CREATE TABLE Table2
                            (
                                Column1     BIGINT
                            )
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenTwoColumnHasDifferentCasing_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                ▶️AJ5055💛script_0.sql💛MyDb.dbo.Table1💛Column1💛COLUMN1, Column1💛MyDb.dbo.Table1.Column1, MyDb.dbo.Table2.COLUMN1✅Column1  INT◀️
                            )
                            GO

                            CREATE TABLE Table2
                            (
                                COLUMN1  INT
                            )
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenTreeColumnHasDifferentCasing_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                ▶️AJ5055💛script_0.sql💛MyDb.dbo.Table1💛Column1💛COLUMN1, CoLuMn1, Column1💛MyDb.dbo.Table1.Column1, MyDb.dbo.Table2.COLUMN1, MyDb.dbo.Table3.CoLuMn1✅Column1  INT◀️
                            )
                            GO

                            CREATE TABLE Table2
                            (
                                COLUMN1  INT
                            )

                            CREATE TABLE Table3
                            (
                                CoLuMn1  INT
                            )
                            """;
        Verify(code);
    }
}
