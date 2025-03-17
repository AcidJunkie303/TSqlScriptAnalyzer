using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Services;
using DatabaseAnalyzer.Common.Settings;
using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Runtime;

public sealed class MissingTableOrViewColumnTests(ITestOutputHelper testOutputHelper)
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
}
