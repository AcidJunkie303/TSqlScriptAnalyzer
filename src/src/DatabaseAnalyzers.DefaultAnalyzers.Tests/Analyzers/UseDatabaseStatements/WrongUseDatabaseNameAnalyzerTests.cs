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
        Verify(sql);
    }

    [Fact]
    public void WhenUsingWrongDatabaseName_ThenDiagnose()
    {
        const string sql = """
                           PRINT 'Hello World'
                           USE [db1]
                           PRINT 'Hello World'
                           {{AJ5003¦main.sql¦¦db1¦master|||USE [master]}}
                           """;
        Verify(sql);
    }

    [Fact]
    public void WhenUsingWrongDatabaseButFileIsExcluded_ThenOk()
    {
        const string sql = """
                           USE master
                           """;

        var settings = new Aj5003SettingsRaw
        {
            ExcludedFilePathPatterns = ["dummy.sql"]
        }.ToSettings();

        var tester = GetDefaultTesterBuilder(sql)
            .WithMainScriptFile(sql, "dummy.sql")
            .WithSettings("AJ5003", settings)
            .Build();

        Verify(tester);
    }
}
