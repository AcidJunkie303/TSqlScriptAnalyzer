using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UnreferencedObject;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.UnreferencedObject;

public sealed class UnreferencedParameterAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<UnreferencedParameterAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WithProcedure_WhenParameterIsReferenced_ThenOk()
    {
        const string sql = """
                           CREATE PROCEDURE [dbo].[P1]
                           	    @Param1 VARCHAR(MAX)
                           AS
                           BEGIN
                           	    PRINT @Param1
                           END
                           """;
        Verify(sql);
    }

    [Fact]
    public void WithProcedure_WhenParameterIsNotReferenced_ThenDiagnose()
    {
        const string sql = """
                           CREATE PROCEDURE [dbo].[P1]
                           	    █AJ5011░main.sql░dbo.P1░@Param1███@Param1 VARCHAR(MAX)█
                           AS
                           BEGIN
                           	    PRINT 'Hello'
                           	    RETURN 1
                           END
                           """;
        Verify(sql);
    }

    [Fact]
    public void WithScalarFunction_WhenParameterIsReferenced_ThenOk()
    {
        const string sql = """
                           CREATE FUNCTION F1
                           (
                               @Param1 VARCHAR(MAX)
                           )
                           RETURNS INT
                           AS
                           BEGIN
                           	    PRINT @Param1
                           	    RETURN 1
                           END
                           """;
        Verify(sql);
    }

    [Fact]
    public void WithScalarFunction_WhenParameterIsNotReferenced_ThenDiagnose()
    {
        const string sql = """
                           CREATE FUNCTION F1
                           (
                               █AJ5011░main.sql░dbo.F1░@Param1███@Param1 VARCHAR(MAX)█
                           )
                           RETURNS INT
                           AS
                           BEGIN
                           	    RETURN 1
                           END
                           """;
        Verify(sql);
    }
}
