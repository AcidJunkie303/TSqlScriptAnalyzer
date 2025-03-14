using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Indices;

public sealed class MissingClusteredIndexAnalyzerTests(ITestOutputHelper testOutputHelper)
    : GlobalAnalyzerTestsBase<MissingClusteredIndexAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenColumnHasPrimaryKeyThroughDirectDeclarationInColumn_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE dbo.Table1
                            (
                                Id            INT NOT NULL PRIMARY KEY CLUSTERED,
                                Value1        NVARCHAR(128) NOT NULL
                            )
                            """;

        Verify(Aj5027Settings.Default, code);
    }

    [Fact]
    public void WhenColumnHasPrimaryKeyThroughConstraintDeclaration_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE dbo.Table1
                            (
                                Id            INT NOT NULL,
                                Value1        NVARCHAR(128) NOT NULL,
                                CONSTRAINT PK_Table1 PRIMARY KEY CLUSTERED
                                (
                                    Id ASC
                                )
                            )
                            """;

        Verify(Aj5027Settings.Default, code);
    }

    [Fact]
    public void WhenTableHasClusteredIndexOnNonKeyColumn_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE dbo.Table1
                            (
                                Id            INT NOT NULL PRIMARY KEY,
                                Value1        NVARCHAR(128) NOT NULL
                            )
                            GO

                            CREATE CLUSTERED INDEX IX_Table1_Value1 ON dbo.Table1
                            (
                                Value1 ASC
                            )
                            """;

        Verify(Aj5027Settings.Default, code);
    }

    [Fact]
    public void WhenTableHasNoClusteredIndex_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            ▶️AJ5027💛script_0.sql💛MyDb.dbo.Table1💛MyDb.dbo.Table1✅CREATE TABLE dbo.Table1
                            (
                                Id            INT NOT NULL,
                                Value1        NVARCHAR(128) NOT NULL
                            )◀️
                            """;

        Verify(Aj5027Settings.Default, code);
    }

    [Fact]
    public void WhenTableHasNoClusteredIndex_WhenTableIsIgnored_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE dbo.Table1
                            (
                                Id            INT NOT NULL,
                                Value1        NVARCHAR(128) NOT NULL
                            )
                            """;

        var settings = new Aj5027SettingsRaw { FullTableNamesToIgnore = ["MyDb.dbo.Table*"] }.ToSettings();

        Verify(settings, code);
    }

    [Fact]
    public void WhenTempTable_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE #T
                            (
                                Id            INT NOT NULL,
                                Value1        NVARCHAR(128) NOT NULL
                            )
                            """;

        VerifyWithDefaultSettings<Aj5027Settings>(code);
    }
}
