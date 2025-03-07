using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime.MissingObjectAnalyzers;
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

        Verify(Settings, SharedCodeForTables, code);
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

        Verify(Settings, SharedCodeForTables, code);
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

        Verify(Settings, SharedCodeForTables, code);
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

        Verify(Settings, SharedCodeForTables, code);
    }
}
