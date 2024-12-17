using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Naming;

public sealed class ParameterReferenceWithDifferentCasingAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<ParameterReferenceWithDifferentCasingAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WithProcedure_WhenParameterReferenceHasSameCasing_ThenOk()
    {
        const string code = """
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
    public void WithProcedure_WhenParameterReferenceHasDifferentCasing_ThenDiagnose()
    {
        const string code = """
                            CREATE PROCEDURE [dbo].[P1]
                                    @Param1 VARCHAR(MAX)
                            AS
                            BEGIN
                                 PRINT █AJ5013░main.sql░dbo.P1░@PARAM1░@Param1███@PARAM1█
                            END
                            """;
        Verify(code);
    }

    [Fact]
    public void WithScalarFunction_WhenParameterReferenceHasSameCasing_ThenOk()
    {
        const string code = """
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
    public void WithScalarFunction_WhenParameterReferenceHasDifferentCasing_ThenDiagnose()
    {
        const string code = """
                            CREATE FUNCTION F1
                            (
                                @Param1 VARCHAR(MAX)
                            )
                            RETURNS INT
                            AS
                            BEGIN
                                    PRINT █AJ5013░main.sql░dbo.F1░@PARAM1░@Param1███@PARAM1█
                                    RETURN 1
                            END
                            """;
        Verify(code);
    }
}
