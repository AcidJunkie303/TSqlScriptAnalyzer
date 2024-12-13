using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.ObjectCreation;

public sealed class ObjectCreationWithoutOrAlterAnalyzerTests(ITestOutputHelper testOutputHelper) : ScriptAnalyzerTestsBase<ObjectCreationWithoutOrAlterAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenCreatingView_WhenOrAlterIsSpecified_ThenOk()
    {
        const string sql = """
                           CREATE OR ALTER VIEW dbo.V1
                           AS
                           SELECT 1 AS Expr1
                           """;

        Verify(sql);
    }

    [Fact]
    public void WhenCreatingView_WhenNoOrAlterIsSpecified_ThenDiagnose()
    {
        const string sql = """
                           {{AJ5009¦main.sql¦dbo.V1|||CREATE VIEW dbo.V1
                           AS
                           SELECT 1 AS Expr1}}
                           """;

        Verify(sql);
    }

    [Fact]
    public void WhenCreatingProcedure_WhenOrAlterIsSpecified_ThenOk()
    {
        const string sql = """
                           CREATE OR ALTER PROCEDURE P1
                           AS
                           BEGIN
                               SELECT 1
                           END
                           """;

        Verify(sql);
    }

    [Fact]
    public void WhenCreatingProcedure_WhenNoOrAlterIsSpecified_ThenDiagnose()
    {
        const string sql = """
                           {{AJ5009¦main.sql¦dbo.P1|||CREATE PROCEDURE P1
                           AS
                           BEGIN
                               SELECT 1
                           END}}
                           """;

        Verify(sql);
    }

    [Fact]
    public void WhenCreatingFunction_WhenOrAlterIsSpecified_ThenOk()
    {
        const string sql = """
                           CREATE OR ALTER FUNCTION F1()
                           RETURNS INT
                           AS
                           BEGIN
                           	    RETURN 1
                           END
                           """;

        Verify(sql);
    }

    [Fact]
    public void WhenCreatingFunction_WhenNoOrAlterIsSpecified_ThenDiagnose()
    {
        const string sql = """
                           {{AJ5009¦main.sql¦dbo.F1|||CREATE FUNCTION F1()
                           RETURNS INT
                           AS
                           BEGIN
                           	    RETURN 1
                           END}}
                           """;

        Verify(sql);
    }

    [Fact]
    public void WhenCreatingClrFunction_WhenOrAlterIsSpecified_ThenOk()
    {
        const string sql = """
                           CREATE OR ALTER PROCEDURE dbo.P1
                           AS EXTERNAL NAME A.B.C;
                           """;

        Verify(sql);
    }

    [Fact]
    public void WhenCreatingClrFunction_WhenNoOrAlterIsSpecified_ThenDiagnose()
    {
        const string sql = """
                           {{AJ5009¦main.sql¦dbo.P1|||CREATE PROCEDURE dbo.P1
                           AS EXTERNAL NAME A.B.C}}
                           """;

        Verify(sql);
    }
}
