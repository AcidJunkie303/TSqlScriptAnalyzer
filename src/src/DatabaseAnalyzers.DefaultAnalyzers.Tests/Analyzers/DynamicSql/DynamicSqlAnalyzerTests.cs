using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.DynamicSql;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.DynamicSql;

public sealed class DynamicSqlAnalyzerTests(ITestOutputHelper testOutputHelper) : ScriptAnalyzerTestsBase<DynamicSqlAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenUsingExecAndCallingStoredProcedure_ThenOk()
    {
        const string sql = """
                           EXEC dbo.P1
                           """;
        Verify(sql);
    }

    [Fact]
    public void WhenUsingExecWithBrackets_ThenDiagnose()
    {
        const string sql = """
                           █AJ5000░main.sql░███EXEC ('SELECT 1')█
                           """;

        Verify(sql);
    }

    [Fact]
    public void WhenUsingExecWithSpExecuteSqlAndCallingStringProvidedCommand_ThenDiagnose()
    {
        const string sql = """
                           █AJ5000░main.sql░███EXEC sp_executeSql 'dbo.P1'█
                           """;

        Verify(sql);
    }

    [Fact]
    public void WhenUsingExecWithSpExecuteSqlAndCallingVariableProvidedCommand_ThenDiagnose()
    {
        const string sql = """
                           DECLARE @sql NVARCHAR = N'SELECT 1'
                           █AJ5000░main.sql░███EXEC sp_executeSql @sql█
                           """;

        Verify(sql);
    }

    [Fact]
    public void WhenUsingDirectProcedureInvocation_ThenOk()
    {
        const string sql = """
                           dbo.P1 @param1 = 123
                           """;

        Verify(sql);
    }
}
