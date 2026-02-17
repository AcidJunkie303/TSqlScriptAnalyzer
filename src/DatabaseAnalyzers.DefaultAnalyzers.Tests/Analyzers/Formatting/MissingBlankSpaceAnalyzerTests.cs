using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

public sealed class MissingBlankSpaceAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<MissingBlankSpaceAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenBlankSpaceAfterComma_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT 1, 2
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenNewLineAfterComma_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  1,
                            2
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenNoBlankSpaceAfterComma_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            -- SELECT 1,2
                            SELECT 1▶️AJ5010💛script_0.sql💛💛after💛,✅,◀️2
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenBlankSpaceBeforeOperator_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO
                            SET @a = 1 + 2
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenNoBlankSpaceBeforeOperator_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            -- SET @a = 1+ 2
                            SET @a = 1▶️AJ5010💛script_0.sql💛💛before💛+✅+◀️ 2
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenNoBlankSpaceAfterOperator_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            -- SET @a = 1 +2
                            SET @a = 1 ▶️AJ5010💛script_0.sql💛💛after💛+✅+◀️2
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenNoBlankSpaceAfterEqualSign_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            -- SET @a =1
                            SET @a ▶️AJ5010💛script_0.sql💛💛after💛=✅=◀️1
                            """;
        Verify(code);
    }

    [Theory]
    [InlineData("<")]
    [InlineData(">")]
    [InlineData("<=")]
    [InlineData(">=")]
    [InlineData("=")]
    [InlineData("<>")]
    [InlineData("!=")]
    public void Theory_ComparisonWithNegativeValue(string comparisonOperator)
    {
        var code = $"""
                    USE MyDb
                    GO

                    IF (@a {comparisonOperator} -1)
                        PRINT 303
                    """;
        Verify(code);
    }

    [Fact]
    public void WhenNegativeValueAsArgument_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                Id  INT NOT NULL IDENTITY(-1, -1)
                            )
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenStartAfterAlias_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  t.*,
                                    1
                            FROM    Table1 t
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenSelectingNegativeValue_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  -1 AS Id
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenNegativeValueAfterThenOrElse_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            DECLARE @a INT =
                                CASE
                                    WHEN 1 = 1 THEN -1
                                    ELSE -2
                                END
                            """;
        Verify(code);
    }
}
