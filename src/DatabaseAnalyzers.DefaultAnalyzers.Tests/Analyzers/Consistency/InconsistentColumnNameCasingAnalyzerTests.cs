using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Consistency;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Consistency;

public sealed class InconsistentColumnNameCasingAnalyzerTests(ITestOutputHelper testOutputHelper)
    : GlobalAnalyzerTestsBase<InconsistentColumnNameCasingAnalyzer>(testOutputHelper)
{
    private static readonly Aj5055Settings Settings = new Aj5055SettingsRaw { ExcludedDatabaseNames = ["IgnoredDb"] }.ToSettings();

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
        Verify(Settings, code);
    }

    [Fact]
    public void WhenTwoColumnHasDifferentCasing_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                ▶️AJ5055💛script_0.sql💛MyDb.dbo.Table1💛Column1💛COLUMN1, Column1💛MyDb.dbo.Table1.Column1, OtherDatabase.dbo.Table2.COLUMN1✅Column1  INT◀️
                            )
                            GO

                            USE OtherDatabase -- other DB

                            CREATE TABLE Table2
                            (
                                COLUMN1  INT
                            )
                            """;
        Verify(Settings, code);
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
        Verify(Settings, code);
    }

    [Fact]
    public void WhenTreeColumnHasDifferentCasing_WhenDatabaseIgnored_ThenOk()
    {
        const string code = """
                            USE AAA
                            GO

                            CREATE TABLE Table1
                            (
                                Column1     INT
                            )
                            GO

                            USE IgnoredDb -- DB is ignored
                            GO

                            CREATE TABLE Table2
                            (
                                CoLuMn1     BIGINT
                            )
                            """;
        Verify(Settings, code);
    }
}
