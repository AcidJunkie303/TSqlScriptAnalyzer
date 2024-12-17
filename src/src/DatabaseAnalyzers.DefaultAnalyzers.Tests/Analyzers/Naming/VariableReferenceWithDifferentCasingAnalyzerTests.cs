using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Naming;

public sealed class VariableReferenceWithDifferentCasingAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<VariableReferenceWithDifferentCasingAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenVariableReferenceHasSameCasing_ThenOk()
    {
        const string code = """
                            DECLARE @Var1 INT = 303
                            PRINT @Var1
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenVariableReferenceHasDifferentCasing_ThenOk()
    {
        const string code = """
                            DECLARE @Var1 INT = 303
                            PRINT █AJ5014░main.sql░░@VAR1░@Var1███@VAR1█
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenVariableIsDefinedInBatch_AndReferencedInDifferentBatchWithDifferentCasing_ThenOK()
    {
        const string code = """
                            DECLARE @Var1 INT

                            GO

                            PRINT @VAR1 -- different casing but in different batch -> ok
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenStoredProcedure_WhenParameterNotUsed_ThenOk()
    {
        // even the parameter is not referenced, we don't care because it is not a variable
        // this is handled by a different analyzer (unreferenced parameter)
        // this is to make sure, we don't intersect the logic
        const string code = """
                            CREATE PROCEDURE P1
                               @Param1 INT
                            AS
                            BEGIN
                                SELECT 1
                            END
                            """;
        Verify(code);
    }
}
