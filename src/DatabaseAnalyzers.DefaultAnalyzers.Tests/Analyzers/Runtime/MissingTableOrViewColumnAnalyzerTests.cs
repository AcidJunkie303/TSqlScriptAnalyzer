using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Services;
using DatabaseAnalyzer.Common.Settings;
using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
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
    public void WhenView_WhenColumnsExist_ThenOk()
    {
        const string local = """
                             USE MyDb
                             GO

                             CREATE VIEW [dbo].[View1]
                             AS
                                 SELECT Id, Column1 AS Name
                                 FROM Table1

                                 UNION ALL

                                 SELECT Id, Column2 AS Name
                                 FROM Table2

                             GO
                             """;

        const string code = """
                            USE MyDb
                            GO

                            SELECT      Id, Name
                            FROM        View1
                            """;

        var tester = GetDefaultTesterBuilder(SharedCode, local, code)
            .WithSettings(Settings)
            .WithService<IAstService>(AstService)
            .Build();

        Verify(tester);
    }

    [Fact]
    public void WhenView_WhenColumnsDoNotExist_ThenDiagnose()
    {
        const string local = """
                             USE MyDb
                             GO

                             CREATE VIEW [dbo].[View1]
                             AS
                                 SELECT Id, Column1 AS Name
                                 FROM Table1

                             GO
                             """;

        const string code = """
                            USE MyDb
                            GO

                            SELECT      Id, ▶️AJ5044💛script_2.sql💛💛column💛MyDb.dbo.View1.Name2✅Name2◀️
                            FROM        View1
                            """;

        var tester = GetDefaultTesterBuilder(SharedCode, local, code)
            .WithSettings(Settings)
            .WithService<IAstService>(AstService)
            .Build();
        Verify(tester);
    }
}
