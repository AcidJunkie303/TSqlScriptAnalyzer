using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.DataTypes;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.DataTypes;

public sealed class DataTypeAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<DataTypeAnalyzer>(testOutputHelper)
{
    private static readonly Aj5006Settings Settings = new Aj5006SettingsRaw
    {
        BannedColumnDataTypes = ["float"], BannedFunctionParameterDataTypes = ["varchar*"], BannedProcedureParameterDataTypes = ["uniqueidentifier"], BannedScriptVariableDataTypes = ["bigint"]
    }.ToSettings();

    [Fact]
    public void WhenCreatingTable_WithoutBannedDataType_ThenOk()
    {
        const string code = """
                            CREATE TABLE Employee
                            (
                                Id INT NOT NULL,
                                Value1 INT NOT NULL
                            );
                            """;

        VerifyWithDefaultSettings<Aj5006Settings>(code);
    }

    [Fact]
    public void WhenCreatingTable_WithBannedDataType_ThenDiagnose()
    {
        const string code = """
                            CREATE TABLE Employee
                            (
                                Id INT NOT NULL,
                                █AJ5006░main.sql░dbo.Employee░FLOAT░table columns███Value1 FLOAT█
                            );
                            """;

        Verify(code, Settings);
    }

    [Fact]
    public void WhenCreatingInlineTableValuedFunction_WithoutBannedDataType_ThenOk()
    {
        const string code = """
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

        VerifyWithDefaultSettings<Aj5006Settings>(code);
    }

    [Fact]
    public void WhenCreatingInlineTableValuedFunction_WithBannedDataType_ThenDiagnose()
    {
        const string code = """
                            CREATE FUNCTION F1
                            (
                                █AJ5006░main.sql░dbo.F1░VARCHAR(MAX)░function parameters███@Param1 VARCHAR(MAX)█
                            )
                            RETURNS TABLE
                            AS
                            RETURN
                            (
                                SELECT 0 as C1
                            )
                            """;

        Verify(code, Settings);
    }

    [Fact]
    public void WhenCreatingMultiStatementTableValuedFunction_WithBannedDataType_ThenDiagnose()
    {
        const string code = """
                            CREATE FUNCTION F1
                            (
                                █AJ5006░main.sql░dbo.F1░VARCHAR(MAX)░function parameters███@Param1 VARCHAR(MAX)█
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

        Verify(code, Settings);
    }

    [Fact]
    public void WhenCreatingScalarValuedFunction_WithBannedDataType_ThenDiagnose()
    {
        const string code = """
                            CREATE FUNCTION F1
                            (
                                █AJ5006░main.sql░dbo.F1░VARCHAR(MAX)░function parameters███@Param1 VARCHAR(MAX)█
                            )
                            RETURNS INT
                            AS
                            BEGIN
                                    RETURN 1
                            END
                            """;

        Verify(code, Settings);
    }

    [Fact]
    public void WhenCreatingProcedure_WithoutBannedDataType_ThenOk()
    {
        const string code = """
                            CREATE PROCEDURE P1
                               @Param1 INT
                            AS
                            BEGIN
                                SELECT 1
                            END
                            """;

        VerifyWithDefaultSettings<Aj5006Settings>(code);
    }

    [Fact]
    public void WhenCreatingProcedure_WithBannedDataType_ThenDiagnose()
    {
        const string code = """
                            CREATE PROCEDURE P1
                                █AJ5006░main.sql░dbo.P1░UNIQUEIDENTIFIER░procedure parameters███@Param1 UNIQUEIDENTIFIER█
                            AS
                            BEGIN
                                SELECT 1
                            END
                            """;

        Verify(code, Settings);
    }

    [Fact]
    public void WhenVariableDeclaration_WithoutBannedDataType_ThenOk()
    {
        const string code = """
                            DECLARE @Var INT
                            """;

        Verify(code, Settings);
    }

    [Fact]
    public void WhenVariableDeclaration_WithBannedDataType_ThenDiagnose()
    {
        // since the provided parser doesn't support CLR stored procedures, and we are doing the parsing our own in a simple way,
        // we use the whole statement as code region
        const string code = """
                            DECLARE █AJ5006░main.sql░░BIGINT░variables███@Var BIGINT█
                            """;

        Verify(code, Settings);
    }
}
