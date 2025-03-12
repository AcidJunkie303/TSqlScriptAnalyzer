using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.NonStandard;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.NonStandard;

public sealed class ReservedWordUsageAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<ReservedWordUsageAnalyzer>(testOutputHelper)
{
    private static readonly Aj5060Settings Settings = new Aj5060SettingsRaw
    {
        ReservedIdentifierNames = ["user"]
    }.ToSettings();

    [Theory]
    [InlineData("Table1")]
    [InlineData("▶️AJ5060💛script_0.sql💛MyDb.dbo.User💛table💛User✅[User]◀️")]
    public void Table_Theory(string tableName)
    {
        var code = $"""
                    USE MyDb
                    GO

                    CREATE TABLE {tableName}
                    (
                        Column1 INT
                    )
                    """;

        Verify(Settings, code);
    }

    [Theory]
    [InlineData("Column1")]
    [InlineData("▶️AJ5060💛script_0.sql💛MyDb.dbo.Table1💛column💛User✅[User]◀️")]
    public void Column_Theory(string columnName)
    {
        var code = $"""
                    USE MyDb
                    GO

                    CREATE TABLE Table1
                    (
                        {columnName} INT
                    )
                    """;

        Verify(Settings, code);
    }

    [Theory]
    [InlineData("Table1")]
    [InlineData("▶️AJ5060💛script_0.sql💛MyDb.dbo.User💛view💛User✅[User]◀️")]
    public void View_Theory(string tableName)
    {
        var code = $"""
                    USE MyDb
                    GO

                    CREATE VIEW {tableName}
                    AS
                        SELECT 1 AS Expr1
                    """;

        Verify(Settings, code);
    }

    [Theory]
    [InlineData("MyFunction")]
    [InlineData("▶️AJ5060💛script_0.sql💛MyDb.dbo.User💛function💛User✅[User]◀️")]
    public void ScalarFunction_Theory(string functionName)
    {
        var code = $"""
                    USE MyDb
                    GO

                    CREATE FUNCTION {functionName} ()
                    RETURNS INT
                    AS
                    BEGIN
                            PRINT @Param1
                            RETURN 1

                    END
                    """;

        Verify(Settings, code);
    }

    [Theory]
    [InlineData("MyFunction")]
    [InlineData("▶️AJ5060💛script_0.sql💛MyDb.dbo.User💛function💛User✅[User]◀️")]
    public void TvFunction_Theory(string functionName)
    {
        var code = $"""
                    USE MyDb
                    GO

                    CREATE FUNCTION {functionName} ()
                    RETURNS @Result TABLE
                    (
                        Column1 INT
                    )
                    AS
                    BEGIN
                        RETURN
                    END
                    """;

        Verify(Settings, code);
    }
}
