using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Services;
using DatabaseAnalyzer.Common.Settings;
using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Runtime;

public sealed class MissingTableOrViewAnalyzerTests(ITestOutputHelper testOutputHelper)
    : GlobalAnalyzerTestsBase<MissingTableOrViewAnalyzer>(testOutputHelper)
{
    private const string SharedCodeForTables = """
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

    [Fact]
    public void WhenSimpleSelect_WhenTableExists_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT * FROM [dbo].[Table1]
                            """;

        VerifyLocal(Settings, SharedCodeForTables, code);
    }

    [Fact]
    public void WhenUpdateWithAlias_WhenTableExists_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            UPDATE      t
                            SET         t.Column1 = 303
                            FROM        Table1 t
                            WHERE       t.Id = 1
                            """;

        VerifyLocal(Settings, SharedCodeForTables, code);
    }

    [Fact]
    public void WhenJoin_WhenTableExists_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            UPDATE      t1
                            SET         t1.Column1 = 303
                            FROM        Table1 t1
                            INNER JOIN  Table2 t2 ON  t1.Id = t2.Id
                            WHERE       t2.Column2 = 1
                            """;

        VerifyLocal(Settings, SharedCodeForTables, code);
    }

    [Fact]
    public void WhenJoin_WhenJoinTableDoesNotExist_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            UPDATE      t1
                            SET         t1.Column1 = 303
                            FROM        Table1 t1
                            INNER JOIN  ▶️AJ5044💛script_1.sql💛💛table or view💛MyDb.dbo.Table3✅Table3 t3◀️ ON t1.Id = t3.Id -- Table3 does not exist
                            WHERE       t3.Column3 = 1
                            """;

        VerifyLocal(Settings, SharedCodeForTables, code);
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

        VerifyLocal(Settings, SharedCodeForTables, code);
    }

    private void VerifyLocal(object settings, params string[] scripts)
    {
        var tester = GetDefaultTesterBuilder(scripts)
            .WithSettings(settings)
            .WithService<IAstService>(new AstService(AstServiceSettings.Default))
            .Build();
        Verify(tester);
    }
}
