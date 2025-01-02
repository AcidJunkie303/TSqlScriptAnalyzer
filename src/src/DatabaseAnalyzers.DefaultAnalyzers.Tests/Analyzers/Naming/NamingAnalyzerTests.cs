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
        ColumnNamePattern = "\\AColumn",
        FunctionNamePattern = "\\AFunction",
        ParameterNamePattern = "\\AParameter",
        PrimaryKeyConstraintNamePattern = "\\APK_",
        ProcedureNamePattern = "\\AProcedure",
        TableNamePattern = "\\ATable",
        TriggerNamePattern = "\\ATRG_",
        VariableNamePattern = "\\AVariable",
        ViewNamePattern = "\\AView"
    }.ToSettings();

    [Theory]
    [InlineData("@Parameter303")]
    [InlineData("█AJ5030░script_0.sql░MyDb.dbo.Procedure111░parameter░p░\\AParameter███@p█")]
    [InlineData("█AJ5030░script_0.sql░MyDb.dbo.Procedure111░parameter░parameter303░\\AParameter███@parameter303█")]
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
    [InlineData("█AJ5030░script_0.sql░MyDb.dbo.Tab303░table░Tab303░\\ATable███Tab303█")]
    [InlineData("█AJ5030░script_0.sql░MyDb.dbo.table303░table░table303░\\ATable███table303█")]
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
    [InlineData("█AJ5030░script_0.sql░MyDb.dbo.Table1░column░Col303░\\AColumn███Col303█")]
    [InlineData("█AJ5030░script_0.sql░MyDb.dbo.Table1░column░column303░\\AColumn███column303█")]
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
    [InlineData("█AJ5030░script_0.sql░MyDb.dbo.Proc303░procedure░Proc303░\\AProcedure███Proc303█")]
    [InlineData("█AJ5030░script_0.sql░MyDb.dbo.procedure303░procedure░procedure303░\\AProcedure███procedure303█")]
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
    [InlineData("█AJ5030░script_0.sql░MyDb.dbo.Func303░function░Func303░\\AFunction███Func303█")]
    [InlineData("█AJ5030░script_0.sql░MyDb.dbo.function303░function░function303░\\AFunction███function303█")]
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
    [InlineData("█AJ5030░script_0.sql░MyDb.dbo.T_303░trigger░T_303░\\ATRG_███T_303█")]
    [InlineData("█AJ5030░script_0.sql░MyDb.dbo.trg_303░trigger░trg_303░\\ATRG_███trg_303█")]
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
    [InlineData("PK_Table1")]
    [InlineData("█AJ5030░script_0.sql░MyDb.dbo.Table1░primary key constraint░PK░\\APK_███PK█")]
    [InlineData("█AJ5030░script_0.sql░MyDb.dbo.Table1░primary key constraint░pk_Table1░\\APK_███pk_Table1█")]
    public void PrimaryKeyConstraintName_Theory(string primaryKeyIndexName)
    {
        //
        var code = $"""
                    USE MyDb
                    GO

                    CREATE TABLE Table1
                    (
                        Column1 INT IDENTITY(1, 1),
                        CONSTRAINT {primaryKeyIndexName} PRIMARY KEY CLUSTERED
                        (
                            Column1 ASC
                        )
                    );
                    """;
        Verify(Settings, code);
    }

    [Theory]
    [InlineData("@Variable303")]
    [InlineData("█AJ5030░script_0.sql░░variable░Var303░\\AVariable███@Var303█")]
    [InlineData("█AJ5030░script_0.sql░░variable░variable303░\\AVariable███@variable303█")]
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
    [InlineData("█AJ5030░script_0.sql░MyDb.dbo.V303░view░V303░\\AView███V303█")]
    [InlineData("█AJ5030░script_0.sql░MyDb.dbo.view303░view░view303░\\AView███view303█")]
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
}
