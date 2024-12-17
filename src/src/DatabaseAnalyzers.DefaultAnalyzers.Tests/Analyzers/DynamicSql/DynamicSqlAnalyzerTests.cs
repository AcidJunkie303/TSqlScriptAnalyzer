using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.DynamicSql;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.DynamicSql;

public sealed class DynamicSqlAnalyzerTests(ITestOutputHelper testOutputHelper) : ScriptAnalyzerTestsBase<DynamicSqlAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenUsingExecAndCallingStoredProcedure_ThenOk()
    {
        const string code = """
                            EXEC dbo.P1
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenUsingExecWithBrackets_ThenDiagnose()
    {
        const string code = """
                            █AJ5000░main.sql░███EXEC ('SELECT 1')█
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenUsingExecWithSpExecuteSqlAndCallingStringProvidedCommand_ThenDiagnose()
    {
        const string code = """
                            █AJ5000░main.sql░███EXEC sp_executeSql 'dbo.P1'█
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenUsingExecWithSpExecuteSqlAndCallingVariableProvidedCommand_ThenDiagnose()
    {
        const string code = """
                            DECLARE @sql NVARCHAR = N'SELECT 1'
                            █AJ5000░main.sql░███EXEC sp_executeSql @sql█
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenUsingDirectProcedureInvocation_ThenOk()
    {
        const string code = """
                            dbo.P1 @param1 = 123
                            """;

        Verify(code);
    }
}
