using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.DataTypes;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.DataTypes;

public sealed class DataTypeAnalyzerTests(ITestOutputHelper testOutputHelper) : ScriptAnalyzerTestsBase<DataTypeAnalyzer>(testOutputHelper)
{
    private static readonly Aj5006Settings Settings = new Aj5006SettingsRaw
    {
        BannedColumnDataTypes = ["float"], BannedFunctionParameterDataTypes = ["varchar"], BannedProcedureParameterDataTypes = ["uniqueidentifier"]
    }.ToSettings();

    [Fact]
    public void WhenCreatingTable_WithoutBannedDataType_ThenOk()
    {
        const string sql = """
                           CREATE TABLE Employee
                           (
                               Id INT NOT NULL,
                               Value1 INT NOT NULL
                           );
                           """;

        VerifyWithDefaultSettings<Aj5006Settings>(sql);
    }

    [Fact]
    public void WhenCreatingTable_WithBannedDataType_ThenDiagnose()
    {
        const string sql = """
                           CREATE TABLE Employee
                           (
                               Id INT NOT NULL,
                               {{AJ5006¦main.sql¦dbo.Employee¦FLOAT¦tables|||Value1 FLOAT}}
                           );
                           """;

        Verify(sql, Settings);
    }

    [Fact]
    public void WhenCreatingFunction_WithoutBannedDataType_ThenOk()
    {
        const string sql = """
                           CREATE FUNCTION dbo.F1
                           (
                               @Param1 INT
                           )
                           RETURNS TABLE
                           AS
                           RETURN
                           (
                               SELECT 1 as C1
                           )
                           """;

        VerifyWithDefaultSettings<Aj5006Settings>(sql);
    }

    [Fact]
    public void WhenCreatingInlineTableValuedFunction_WithBannedDataType_ThenDiagnose()
    {
        const string sql = """
                           CREATE FUNCTION F1
                           (
                               {{AJ5006¦main.sql¦dbo.F1¦VARCHAR(MAX)¦functions|||@Param1 VARCHAR(MAX)}}
                           )
                           RETURNS TABLE
                           AS
                           RETURN
                           (
                               SELECT 0 as C1
                           )
                           """;

        Verify(sql, Settings);
    }

    [Fact]
    public void WhenCreatingMultiStatementTableValuedFunction_WithBannedDataType_ThenDiagnose()
    {
        const string sql = """
                           CREATE FUNCTION F1
                           (
                           	   {{AJ5006¦main.sql¦dbo.F1¦VARCHAR(MAX)¦functions|||@Param1 VARCHAR(MAX)}}
                           )
                           RETURNS @Result TABLE
                           (
                           	   Column1 INT
                           )
                           AS
                           BEGIN
                               RETURN
                           END
                           """;

        Verify(sql, Settings);
    }

    [Fact]
    public void WhenCreatingScalarValuedFunction_WithBannedDataType_ThenDiagnose()
    {
        const string sql = """
                           CREATE FUNCTION F1
                           (
                               {{AJ5006¦main.sql¦dbo.F1¦VARCHAR(MAX)¦functions|||@Param1 VARCHAR(MAX)}}
                           )
                           RETURNS INT
                           AS
                           BEGIN
                           	    RETURN 1
                           END
                           """;

        Verify(sql, Settings);
    }

    [Fact]
    public void WhenCreatingProcedure_WithoutBannedDataType_ThenOk()
    {
        const string sql = """
                           CREATE PROCEDURE P1
                              @Param1 INT
                           AS
                           BEGIN
                               SELECT 1
                           END
                           """;

        VerifyWithDefaultSettings<Aj5006Settings>(sql);
    }

    [Fact]
    public void WhenCreatingProcedure_WithBannedDataType_ThenDiagnose()
    {
        const string sql = """
                           CREATE PROCEDURE P1
                               {{AJ5006¦main.sql¦dbo.P1¦UNIQUEIDENTIFIER¦procedures|||@Param1 UniqueIdentifier}}
                           AS
                           BEGIN
                               SELECT 1
                           END
                           """;

        Verify(sql, Settings);
    }

    [Fact]
    public void WhenCreatingClrProcedure_WithoutBannedDataType_ThenDiagnose()
    {
        const string sql = """
                           CREATE  PROCEDURE dbo.P1
                               @Param1 INT
                           WITH EXECUTE AS OWNER
                           AS EXTERNAL NAME A.B.C;
                           """;

        Verify(sql, Settings);
    }

    [Fact]
    public void WhenCreatingClrProcedure_WithBannedDataType_ThenDiagnose()
    {
        // since the provided parser doesn't support CLR stored procedures, and we are doing the parsing our own in a simple way,
        // we use the whole statement as code region
        const string sql = """
                           {{AJ5006¦main.sql¦dbo.P1¦UNIQUEIDENTIFIER¦procedures|||CREATE  PROCEDURE dbo.P1
                               @Param1 uniqueidentifier
                           WITH EXECUTE AS OWNER
                           AS EXTERNAL NAME A.B.C}}
                           """;

        Verify(sql, Settings);
    }
}
