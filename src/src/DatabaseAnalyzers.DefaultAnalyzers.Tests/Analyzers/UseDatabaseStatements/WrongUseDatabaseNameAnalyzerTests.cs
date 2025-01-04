using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UseDatabaseStatements;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.UseDatabaseStatements;

public sealed class WrongUseDatabaseNameAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<WrongUseDatabaseNameAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenUsingCorrectDatabaseName_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            PRINT 'Hello World'
                            """;

        VerifyWithDefaultSettings<Aj5003Settings>(code);
    }

    [Fact]
    public void WhenUsingWrongDatabaseName_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            PRINT 'Hello World'

                            ▶️AJ5003💛script_0.sql💛💛master💛MyDb✅USE [master]◀️
                            PRINT 'Hello World'
                            """;

        VerifyWithDefaultSettings<Aj5003Settings>(code);
    }

    [Fact]
    public void WhenUsingWrongDatabaseButFileIsExcluded_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            USE master
                            """;

        var settings = new Aj5003SettingsRaw
        {
            ExcludedFilePathPatterns = ["script_0.sql"]
        }.ToSettings();

        Verify(settings, code);
    }
}
