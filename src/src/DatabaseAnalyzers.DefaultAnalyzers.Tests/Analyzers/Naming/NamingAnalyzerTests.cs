using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Naming;

public sealed class NamingAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<NamingAnalyzer>(testOutputHelper)
{
    private static readonly Aj5030Settings Settings = new Aj5030SettingsRaw
    {
        ColumnName = new Aj5030SettingsRaw.PatternEntryRaw { Pattern = "\\AColumn", Description = "AAA" },
        FunctionName = new Aj5030SettingsRaw.PatternEntryRaw { Pattern = "\\AFunction", Description = "BBB" },
        ParameterName = new Aj5030SettingsRaw.PatternEntryRaw { Pattern = "\\AParameter", Description = "CCC" },
        PrimaryKeyConstraintName = new Aj5030SettingsRaw.PatternEntryRaw { Pattern = "\\APK_", Description = "DDD" },
        ProcedureName = new Aj5030SettingsRaw.PatternEntryRaw { Pattern = "\\AProcedure", Description = "EEE" },
        TableName = new Aj5030SettingsRaw.PatternEntryRaw { Pattern = "\\ATable", Description = "FFF" },
        TempTableName = new Aj5030SettingsRaw.PatternEntryRaw { Pattern = "\\A##?Table", Description = "GGG" },
        TriggerName = new Aj5030SettingsRaw.PatternEntryRaw { Pattern = "\\ATRG_", Description = "HHH" },
        VariableName = new Aj5030SettingsRaw.PatternEntryRaw { Pattern = "\\AVariable", Description = "III" },
        ViewName = new Aj5030SettingsRaw.PatternEntryRaw { Pattern = "\\AView", Description = "JJJ" },
        IgnoredObjectNamePatterns = ["OtherDb.*"]
    }.ToSettings();

    [Theory]
    [InlineData("@Parameter303")]
    [InlineData("▶️AJ5030💛script_0.sql💛MyDb.dbo.Procedure111💛parameter💛@p💛CCC✅@p◀️")]
    [InlineData("▶️AJ5030💛script_0.sql💛MyDb.dbo.Procedure111💛parameter💛@parameter303💛CCC✅@parameter303◀️")]
    public void ProcedureParameterName_Theory(string parameterName)
    {
        var code = $"""
                    USE MyDb
                    GO

                    CREATE PROCEDURE dbo.Procedure111
                        {parameterName} INT
                    AS
                    BEGIN
                        PRINT @Param1
                    END
                    """;
        Verify(Settings, code);
    }

    [Theory]
    [InlineData("Table303")]
    [InlineData("▶️AJ5030💛script_0.sql💛MyDb.dbo.Tab303💛table💛Tab303💛FFF✅Tab303◀️")]
    [InlineData("▶️AJ5030💛script_0.sql💛MyDb.dbo.table303💛table💛table303💛FFF✅table303◀️")]
    public void TableName_Theory(string tableName)
    {
        var code = $"""
                    USE MyDb
                    GO

                    CREATE TABLE dbo.{tableName}
                    (
                        Column303        NVARCHAR(128) NOT NULL
                    )
                    """;
        Verify(Settings, code);
    }

    [Theory]
    [InlineData("Column303")]
    [InlineData("▶️AJ5030💛script_0.sql💛MyDb.dbo.Table1💛column💛Col303💛AAA✅Col303◀️")]
    [InlineData("▶️AJ5030💛script_0.sql💛MyDb.dbo.Table1💛column💛column303💛AAA✅column303◀️")]
    public void TableColumnName_Theory(string columnName)
    {
        var code = $"""
                    USE MyDb
                    GO

                    CREATE TABLE dbo.Table1
                    (
                        {columnName}   NVARCHAR(128) NOT NULL
                    )
                    """;
        Verify(Settings, code);
    }

    [Theory]
    [InlineData("Procedure303")]
    [InlineData("▶️AJ5030💛script_0.sql💛MyDb.dbo.Proc303💛procedure💛Proc303💛EEE✅Proc303◀️")]
    [InlineData("▶️AJ5030💛script_0.sql💛MyDb.dbo.procedure303💛procedure💛procedure303💛EEE✅procedure303◀️")]
    public void ProcedureName_Theory(string procedureName)
    {
        var code = $"""
                    USE MyDb
                    GO

                    CREATE PROCEDURE dbo.{procedureName}
                    AS
                    BEGIN
                        PRINT @Param1
                    END
                    """;
        Verify(Settings, code);
    }

    [Theory]
    [InlineData("Function303")]
    [InlineData("▶️AJ5030💛script_0.sql💛MyDb.dbo.Func303💛function💛Func303💛BBB✅Func303◀️")]
    [InlineData("▶️AJ5030💛script_0.sql💛MyDb.dbo.function303💛function💛function303💛BBB✅function303◀️")]
    public void FunctionName_Theory(string procedureName)
    {
        var code = $"""
                    USE MyDb
                    GO

                    CREATE FUNCTION dbo.{procedureName} ()
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
    [InlineData("TRG_303")]
    [InlineData("▶️AJ5030💛script_0.sql💛MyDb.dbo.T_303💛trigger💛T_303💛HHH✅T_303◀️")]
    [InlineData("▶️AJ5030💛script_0.sql💛MyDb.dbo.trg_303💛trigger💛trg_303💛HHH✅trg_303◀️")]
    public void TriggerName_Theory(string tiggerName)
    {
        var code = $"""
                    USE MyDb
                    GO

                    CREATE TRIGGER dbo.{tiggerName}
                       ON dbo.Table1
                       AFTER INSERT
                    AS
                    BEGIN
                        PRINT 'Hello'
                    END
                    """;
        Verify(Settings, code);
    }

    [Theory]
    [InlineData("@Variable303")]
    [InlineData("▶️AJ5030💛script_0.sql💛💛variable💛@Var303💛III✅@Var303◀️")]
    [InlineData("▶️AJ5030💛script_0.sql💛💛variable💛@variable303💛III✅@variable303◀️")]
    public void VariableName_Theory(string variableName)
    {
        //
        var code = $"""
                    USE MyDb
                    GO

                    DECLARE {variableName} INT
                    """;
        Verify(Settings, code);
    }

    [Theory]
    [InlineData("View303")]
    [InlineData("▶️AJ5030💛script_0.sql💛MyDb.dbo.V303💛view💛V303💛JJJ✅V303◀️")]
    [InlineData("▶️AJ5030💛script_0.sql💛MyDb.dbo.view303💛view💛view303💛JJJ✅view303◀️")]
    public void ViewName_Theory(string viewName)
    {
        //
        var code = $"""
                    USE MyDb
                    GO

                    CREATE VIEW dbo.{viewName}
                    AS
                    SELECT 1 AS Expr1
                    """;
        Verify(Settings, code);
    }

    [Theory]
    [InlineData("/* 0001 */ #Table303")]
    [InlineData("/* 0002 */ ##Table303")]
    [InlineData("/* 0003 */ ▶️AJ5030💛script_0.sql💛💛temp-table💛#Tab303💛GGG✅#Tab303◀️")]
    [InlineData("/* 0004 */ ▶️AJ5030💛script_0.sql💛💛temp-table💛##Tab303💛GGG✅##Tab303◀️")]
    [InlineData("/* 0005 */ ▶️AJ5030💛script_0.sql💛💛temp-table💛#table303💛GGG✅#table303◀️")]
    [InlineData("/* 0006 */ ▶️AJ5030💛script_0.sql💛💛temp-table💛##table303💛GGG✅##table303◀️")]
    public void TempTableName_Theory(string tempTableName)
    {
        var code = $"""
                    USE MyDb
                    GO

                    CREATE TABLE {tempTableName}
                    (
                        Column303        NVARCHAR(128) NOT NULL
                    )
                    """;
        Verify(Settings, code);
    }

    [Fact]
    public void WhenObjectIgnored_ThenOk()
    {
        const string code = """
                            USE OtHerDb
                            GO

                            CrEaTe tAbLe TaBlE1
                            (
                                CoLuMn1        NVARCHAR(128) NOT NULL
                            )
                            """;

        Verify(Settings, code);
    }
}
