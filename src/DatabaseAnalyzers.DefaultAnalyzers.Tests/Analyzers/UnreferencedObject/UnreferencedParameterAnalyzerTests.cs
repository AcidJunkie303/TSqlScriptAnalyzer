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
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                                @Param1 VARCHAR(MAX)
                            AS
                            BEGIN
                                PRINT @Param1
                            END
                            """;
        Verify(code);
    }

    [Fact]
    public void WithProcedure_WhenParameterIsNotReferenced_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            CREATE PROCEDURE [dbo].[P1]
                                ▶️AJ5011💛script_0.sql💛MyDb.dbo.P1💛@Param1✅@Param1 VARCHAR(MAX)◀️
                            AS
                            BEGIN
                                    PRINT 'Hello'
                                    RETURN 1
                            END
                            """;
        Verify(code);
    }

    [Fact]
    public void WithScalarFunction_WhenParameterIsReferenced_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO
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
        Verify(code);
    }

    [Fact]
    public void WithScalarFunction_WhenParameterIsNotReferenced_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            CREATE FUNCTION F1
                            (
                                ▶️AJ5011💛script_0.sql💛MyDb.dbo.F1💛@Param1✅@Param1 VARCHAR(MAX)◀️
                            )
                            RETURNS INT
                            AS
                            BEGIN
                                    RETURN 1
                            END
                            """;
        Verify(code);
    }
}
