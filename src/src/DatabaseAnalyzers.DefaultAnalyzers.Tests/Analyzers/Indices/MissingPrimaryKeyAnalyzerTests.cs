using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Indices;

public sealed class MissingPrimaryKeyAnalyzerTests(ITestOutputHelper testOutputHelper)
    : GlobalAnalyzerTestsBase<MissingPrimaryKeyAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenColumnHasPrimaryKeyThroughDirectDeclaration_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                Id            INT NOT NULL PRIMARY KEY,
                                Value1        NVARCHAR(128) NOT NULL
                            )
                            """;

        Verify(Aj5026Settings.Default, code);
    }

    [Fact]
    public void WhenColumnHasPrimaryKeyThroughConstraint_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                Id              INT NOT NULL PRIMARY KEY,
                                Value1          NVARCHAR(128) NOT NULL,
                                CONSTRAINT      PK_Table1 PRIMARY KEY (Id)
                            )
                            """;

        Verify(Aj5026Settings.Default, code);
    }

    [Fact]
    public void WhenNoPrimaryKey_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            █AJ5026░script_0.sql░MyDb.dbo.Table1░MyDb.dbo.Table1███CREATE TABLE Table1
                            (
                                Id              INT NOT NULL,
                                Value1          NVARCHAR(128) NOT NULL
                            )█
                            """;

        Verify(Aj5026Settings.Default, code);
    }

    [Fact]
    public void WhenNoPrimaryKey_ButTableIgnored_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                Id              INT NOT NULL,
                                Value1          NVARCHAR(128) NOT NULL
                            )
                            """;

        var settings = new Aj5026SettingsRaw { FullTableNamesToIgnore = ["MyDb.dbo.Table*"] }.ToSettings();

        Verify(settings, code);
    }
}
