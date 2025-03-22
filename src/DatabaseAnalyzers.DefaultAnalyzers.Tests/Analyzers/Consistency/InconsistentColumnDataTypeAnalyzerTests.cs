using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Consistency;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Consistency;

public sealed class InconsistentColumnDataTypeAnalyzerTests(ITestOutputHelper testOutputHelper)
    : GlobalAnalyzerTestsBase<InconsistentColumnDataTypeAnalyzer>(testOutputHelper)
{
    private static readonly Aj5054Settings Settings = new Aj5054SettingsRaw
    {
        DatabasesToExclude = ["Ignored-DB"],
        ColumnNamesToExclude = ["Id"]
    }.ToSettings();

    [Fact]
    public void WhenColumnHasDifferentDataTypes_ButColumnIsIgnored_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                Id      INT NOT NULL
                            )
                            GO

                            CREATE TABLE Table2
                            (
                                Id      BIGINT NOT NULL
                            )
                            """;
        Verify(Settings, code);
    }

    [Fact]
    public void WhenColumnHaveNoDifferentDataType_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                Column1  NVARCHAR (MAX)
                            )
                            GO

                            CREATE TABLE Table2
                            (
                                Column1  nvarchar ( max )
                            )
                            """;
        Verify(Settings, code);
    }

    [Fact]
    public void WhenColumnsHaveDifferentDataType_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                ▶️AJ5054💛script_0.sql💛MyDb.dbo.Table1💛Column1💛[BIGINT], [INT]💛MyDb.dbo.Table1.Column1, MyDb.dbo.Table2.Column1✅Column1      INT NOT NULL◀️
                            )
                            GO

                            CREATE TABLE Table2
                            (
                                Column1      BIGINT NOT NULL
                            )
                            """;

        Verify(Settings, code);
    }

    [Fact]
    public void WhenColumnsHaveDifferentDataType_WhenDifferentDatabases_ThenDiagnose()
    {
        const string code = """
                            USE MyDb1
                            GO

                            CREATE TABLE Table1
                            (
                                ▶️AJ5054💛script_0.sql💛MyDb1.dbo.Table1💛Column1💛[BIGINT], [INT]💛MyDb1.dbo.Table1.Column1, MyDb2.dbo.Table2.Column1✅Column1      INT NOT NULL◀️
                            )
                            GO

                            USE MyDb2
                            GO

                            CREATE TABLE Table2
                            (
                                Column1      BIGINT NOT NULL
                            )
                            """;

        Verify(Settings, code);
    }
}
