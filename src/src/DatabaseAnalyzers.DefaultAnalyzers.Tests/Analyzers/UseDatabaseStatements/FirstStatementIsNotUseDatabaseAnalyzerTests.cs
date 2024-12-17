using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UseDatabaseStatements;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.UseDatabaseStatements;

public sealed class FirstStatementIsNotUseDatabaseAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<FirstStatementIsNotUseDatabaseAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenFirstStatementIsUseDatabase_ThenOk()
    {
        const string code = """
                            USE DB1
                            PRINT 'Hello World'
                            """;
        Verify(code);
    }

    [Fact]
    public void WithLeadingComments_WhenFirstStatementIsUseDatabase_ThenOk()
    {
        const string code = """
                            -- comments
                            /*
                                comments
                            */
                            USE DB1
                            PRINT 'Hello World'
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenFirstStatementIsNotUseDatabase_ThenDiagnose()
    {
        const string code = """
                            █AJ5004░main.sql░░db1███PRINT 'Hello World'█
                            USE DB1
                            """;
        Verify(code);
    }
}
