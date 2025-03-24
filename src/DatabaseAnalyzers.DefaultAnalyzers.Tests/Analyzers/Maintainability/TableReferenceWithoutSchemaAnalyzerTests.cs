using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Maintainability;

public sealed class TableReferenceWithoutSchemaAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<TableReferenceWithoutSchemaAnalyzer>(testOutputHelper)
{
    private static readonly Aj5066Settings TableAbcIsIgnoredSettings = new Aj5066SettingsRaw
    {
        IgnoredTableNames = ["TABLEabc"]
    }.ToSettings();

    [Fact]
    public void WhenSchemaSpecified_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  t2.Value
                            FROM    dbo.Table1      t1
                            INNER   JOIN dbo.Table2 t2 ON t2.Id = t1.Id

                            """;

        Verify(Aj5066Settings.Default, code);
    }

    [Fact]
    public void WhenNoSchemaSpecifiedInFrom_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  t2.Value
                            FROM    ▶️AJ5066💛script_0.sql💛💛Table1✅Table1      t1◀️
                            INNER   JOIN dbo.Table2 t2 ON t2.Id = t1.Id

                            """;

        Verify(Aj5066Settings.Default, code);
    }

    [Fact]
    public void WhenNoSchemaSpecifiedJoin_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  t2.Value
                            FROM    dbo.Table1      t1
                            INNER   JOIN ▶️AJ5066💛script_0.sql💛💛Table2✅Table2 t2◀️ ON t2.Id = t1.Id

                            """;

        Verify(Aj5066Settings.Default, code);
    }

    [Fact]
    public void WhenSchemaSpecified_ButTableIsIgnored_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  Value
                            FROM    TableAbc

                            """;

        Verify(TableAbcIsIgnoredSettings, code);
    }

    [Fact]
    public void WhenReferenceIsCteInRecursiveCte_ThenOK()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE OR ALTER FUNCTION [dbo].[usf_SplitList](
                              @List      NVARCHAR(MAX),
                              @Delimiter NCHAR(1)
                            )
                            RETURNS @t TABLE (Item NVARCHAR(MAX))
                            AS
                            BEGIN
                              SET @List += @Delimiter;
                              ;WITH a(f, t) AS
                              (
                                SELECT CAST(1 AS BIGINT), CHARINDEX(@Delimiter, @List)
                                UNION ALL
                                SELECT t + 1, CHARINDEX(@Delimiter, @List, t + 1)
                                FROM a WHERE CHARINDEX(@Delimiter, @List, t + 1) > 0  -- AJ5044 raised for `a`
                              )
                              INSERT @t
                              SELECT SUBSTRING(@List, f, t - f)
                              FROM a OPTION (MAXRECURSION 0);   -- AJ5044 raised for `a`
                              RETURN;
                            END

                            """;

        Verify(TableAbcIsIgnoredSettings, code);
    }
}
