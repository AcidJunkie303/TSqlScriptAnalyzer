using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.ObjectCreation;

public sealed class ObjectCreationWithoutOrAlterAnalyzerTests(ITestOutputHelper testOutputHelper) : ScriptAnalyzerTestsBase<ObjectCreationWithoutOrAlterAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenCreatingView_WhenOrAlterIsSpecified_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO
                            CREATE OR ALTER VIEW dbo.V1
                            AS
                            SELECT 1 AS Expr1
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenCreatingView_WhenNoOrAlterIsSpecified_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            █AJ5009░script_0.sql░MyDb.dbo.V1███CREATE VIEW dbo.V1
                            AS
                            SELECT 1 AS Expr1█
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenCreatingProcedure_WhenOrAlterIsSpecified_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO
                            CREATE OR ALTER PROCEDURE P1
                            AS
                            BEGIN
                                SELECT 1
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenCreatingProcedure_WhenNoOrAlterIsSpecified_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            █AJ5009░script_0.sql░MyDb.dbo.P1███CREATE PROCEDURE P1
                            AS
                            BEGIN
                                SELECT 1
                            END█
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenCreatingFunction_WhenOrAlterIsSpecified_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO
                            CREATE OR ALTER FUNCTION F1()
                            RETURNS INT
                            AS
                            BEGIN
                                    RETURN 1
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenCreatingFunction_WhenNoOrAlterIsSpecified_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            █AJ5009░script_0.sql░MyDb.dbo.F1███CREATE FUNCTION F1()
                            RETURNS INT
                            AS
                            BEGIN
                                    RETURN 1
                            END█
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenCreatingClrFunction_WhenOrAlterIsSpecified_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE OR ALTER PROCEDURE dbo.P1
                            AS EXTERNAL NAME A.B.C;
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenCreatingClrFunction_WhenNoOrAlterIsSpecified_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            █AJ5009░script_0.sql░MyDb.dbo.P1███CREATE PROCEDURE dbo.P1
                            AS EXTERNAL NAME A.B.C█
                            """;

        Verify(code);
    }
}
