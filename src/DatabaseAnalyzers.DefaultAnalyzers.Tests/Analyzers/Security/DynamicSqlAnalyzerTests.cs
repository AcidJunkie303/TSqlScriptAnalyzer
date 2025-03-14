using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Security;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Security;

public sealed class DynamicSqlAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<DynamicSqlAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenDirectProcedureCall1_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            EXEC dbo.P1
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenDirectProcedureCall2_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            dbo.P1 @param1 = 123
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenVariable_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            ▶️AJ5000💛script_0.sql💛✅EXEC (@cmd)◀️
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenStringLiteral_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            EXEC ('SELECT 1')
                            """;

        Verify(code);
    }

    [Fact]
    public void WithSpExecuteSql_WhenStringLiteral_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            EXEC sp_executeSql 'dbo.P1'
                            """;

        Verify(code);
    }

    [Fact]
    public void WithSpExecuteSql_WhenVariable_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            DECLARE @sql NVARCHAR = N'SELECT 1'
                            ▶️AJ5000💛script_0.sql💛✅EXEC sp_executeSql @sql◀️
                            """;

        Verify(code);
    }

    [Fact]
    public void WithExec_WhenDirect_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            EXEC dbo.usp_ProcessLog @Variable1, @Variable2
                            """;

        Verify(code);
    }
}
