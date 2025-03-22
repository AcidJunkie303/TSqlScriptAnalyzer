using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

public sealed class IntoSingleLineSqueezingAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<IntoSingleLineSqueezingAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WithSelect_WhenColumnsAreOnSeparateLine_ThenOK()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  Column1
                                  , Column2
                            FROM    Table1
                            """;
        Verify(code);
    }

    [Fact]
    public void WithSelect_WhenOnSameLine_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  ▶️AJ5064💛script_0.sql💛💛columns✅Column1, Column2◀️
                            FROM    Table1
                            """;
        Verify(code);
    }

    [Fact]
    public void WithSelect_WhenOnSameLine_WithSeparate_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  ▶️AJ5064💛script_0.sql💛💛columns✅Column1, Column2,
                                    Column3◀️
                            FROM    Table1
                            """;
        Verify(code);
    }

    [Fact]
    public void WithProcedureParameter_WhenOnSameLine_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                                ▶️AJ5064💛script_0.sql💛MyDb.dbo.P1💛parameters✅@Param1 VARCHAR(MAX), @Param2 VARCHAR(MAX)◀️
                            AS
                            BEGIN PRINT 303 END
                            """;
        Verify(code);
    }

    [Fact]
    public void WithProcedureParameter_WhenOnSameLine_WithSeparate_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                                ▶️AJ5064💛script_0.sql💛MyDb.dbo.P1💛parameters✅@Param1 VARCHAR(MAX), @Param2 VARCHAR(MAX),
                                @Param3 VARCHAR(MAX)◀️
                            AS
                            BEGIN PRINT 303 END
                            """;
        Verify(code);
    }

    [Fact]
    public void WithUpdate_WhenOnSameLine_ThenDiagnose()
    {
        // ignored because handled by IntoSingleLineSqueezingAnalyzer
        const string code = """
                            USE MyDb
                            GO

                            UPDATE      Table1
                            SET         ▶️AJ5064💛script_0.sql💛💛columns✅Column1 = 'tb', Column2 = 303◀️
                            """;
        Verify(code);
    }

    [Fact]
    public void WithUpdate_WhenColumnsAreOnSeparateLine_ThenOK()
    {
        const string code = """
                            USE MyDb
                            GO

                            UPDATE      Table1
                            SET         Column1 = 'tb',
                                        Column2 = 303

                            """;
        Verify(code);
    }
}
