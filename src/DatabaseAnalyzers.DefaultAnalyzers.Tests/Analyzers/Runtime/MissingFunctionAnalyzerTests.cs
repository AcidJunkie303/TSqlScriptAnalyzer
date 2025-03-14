using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Runtime;

public sealed class MissingFunctionAnalyzerTests(ITestOutputHelper testOutputHelper)
    : GlobalAnalyzerTestsBase<MissingFunctionAnalyzer>(testOutputHelper)
{
    private const string SharedCodeForProcedures = """
                                                   USE MyDb
                                                   GO

                                                   CREATE FUNCTION dbo.TVF1()
                                                   RETURNS TABLE AS
                                                   RETURN
                                                   (
                                                       SELECT Id, Name
                                                       FROM Student
                                                   )
                                                   GO

                                                   CREATE FUNCTION dbo.SF1()
                                                   RETURNS INT AS
                                                   BEGIN
                                                       RETURN 303
                                                   END
                                                   """;

    private static readonly Aj5044Settings Settings = new Aj5044SettingsRaw
    {
        IgnoredObjectNamePatterns = ["*.ignored.*"]
    }.ToSettings();

    [Fact]
    public void WithTvf_WhenFunctionExists_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  *
                            FROM    dbo.TVF1()
                            """;

        Verify(Settings, SharedCodeForProcedures, code);
    }

    [Fact]
    public void WithTvf_WhenFunctionDoesNotExist_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  *
                            FROM    ▶️AJ5044💛script_1.sql💛💛function💛MyDb.dbo.DoesNotExist✅dbo.DoesNotExist()◀️
                            """;

        Verify(Settings, SharedCodeForProcedures, code);
    }

    [Fact]
    public void WithScalarFunction_WhenFunctionExists_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            PRINT dbo.SF1()
                            """;

        Verify(Settings, SharedCodeForProcedures, code);
    }

    [Fact]
    public void WithScalarFunction_WhenFunctionDoesNotExist_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            PRINT   ▶️AJ5044💛script_1.sql💛💛function💛MyDb.dbo.DoesNotExist✅dbo.DoesNotExist()◀️
                            """;

        Verify(Settings, SharedCodeForProcedures, code);
    }
}
