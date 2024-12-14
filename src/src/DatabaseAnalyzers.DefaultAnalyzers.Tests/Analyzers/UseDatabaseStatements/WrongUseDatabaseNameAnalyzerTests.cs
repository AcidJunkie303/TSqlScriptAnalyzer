using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UseDatabaseStatements;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.UseDatabaseStatements;

public sealed class WrongUseDatabaseNameAnalyzerTests(ITestOutputHelper testOutputHelper) : ScriptAnalyzerTestsBase<WrongUseDatabaseNameAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenUsingCorrectDatabaseName_ThenOk()
    {
        const string sql = """
                           PRINT 'Hello World'
                           USE [db1]
                           """;

        VerifyWithDefaultSettings<Aj5003Settings>(sql);
    }

    [Fact]
    public void WhenUsingWrongDatabaseName_ThenDiagnose()
    {
        const string sql = """
                           PRINT 'Hello World'
                           USE [db1]
                           PRINT 'Hello World'
                           █AJ5003░main.sql░░db1░master███USE [master]█
                           """;

        VerifyWithDefaultSettings<Aj5003Settings>(sql);
    }

    [Fact]
    public void WhenUsingWrongDatabaseButFileIsExcluded_ThenOk()
    {
        const string sql = """
                           USE master
                           """;

        var settings = new Aj5003SettingsRaw
        {
            ExcludedFilePathPatterns = ["main.sql"]
        }.ToSettings();

        Verify(sql, settings);
    }
}
