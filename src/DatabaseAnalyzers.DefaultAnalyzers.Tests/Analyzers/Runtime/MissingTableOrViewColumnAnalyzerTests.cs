using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Services;
using DatabaseAnalyzer.Common.Settings;
using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Runtime;

public sealed class MissingTableOrViewColumnAnalyzerTests(ITestOutputHelper testOutputHelper)
    : GlobalAnalyzerTestsBase<MissingTableOrViewColumnAnalyzer>(testOutputHelper)
{
    private const string SharedCode = """
                                      USE MyDb
                                      GO

                                      CREATE TABLE [dbo].[Table1]
                                      (
                                          Id       INT NOT NULL,
                                          Column1  INT
                                      )

                                      CREATE TABLE [dbo].[Table2]
                                      (
                                          Id       INT NOT NULL,
                                          Column2  INT
                                      )

                                      """;

    private static readonly Aj5044Settings Settings = new Aj5044SettingsRaw
    {
        IgnoredObjectNamePatterns = ["*.ignored.*"]
    }.ToSettings();

    private static readonly IAstService AstService = new AstService(AstServiceSettings.Default);

    [Fact]
    public void WhenSimpleSelect_WhenTableColumnExists_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT Id FROM [dbo].[Table1]
                            """;

        var tester = GetDefaultTesterBuilder(SharedCode, code)
            .WithSettings(Settings)
            .WithService<IAstService>(AstService)
            .Build();
        Verify(tester);
    }

    [Fact]
    public void WhenSimpleSelect_WhenTableColumnDoesNotExist_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  ▶️AJ5044💛script_1.sql💛💛column💛MyDb.dbo.Table1.DoesNotExist✅DoesNotExist◀️
                            FROM    [dbo].[Table1]
                            """;

        var tester = GetDefaultTesterBuilder(SharedCode, code)
            .WithSettings(Settings)
            .WithService<IAstService>(AstService)
            .Build();
        Verify(tester);
    }

    [Fact]
    public void WithJoin_WhenUpdate_WhenColumnExists_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            UPDATE      t1
                            SET         [Column1] = 0
                            FROM        MyDb.dbo.Table1 t1
                            JOIN        Table2 t2 ON t2.Id = t1.Id and t2.Column2 = 303
                            """;

        var tester = GetDefaultTesterBuilder(SharedCode, code)
            .WithSettings(Settings)
            .WithService<IAstService>(AstService)
            .Build();
        Verify(tester);
    }

    [Fact]
    public void WhenTempTable_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      Column1
                            FROM        #temp
                            """;

        var tester = GetDefaultTesterBuilder(SharedCode, code)
            .WithSettings(Settings)
            .WithService<IAstService>(AstService)
            .Build();
        Verify(tester);
    }

    [Fact]
    public void Test()
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
                              INSERT @t SELECT SUBSTRING(@List, f, t - f) FROM a OPTION (MAXRECURSION 0);   -- AJ5044 raised for `a`
                              RETURN;
                            END;
                            """;

        var tester = GetDefaultTesterBuilder(SharedCode, code)
            .WithSettings(Settings)
            .WithService<IAstService>(AstService)
            .Build();
        Verify(tester);
    }
}
