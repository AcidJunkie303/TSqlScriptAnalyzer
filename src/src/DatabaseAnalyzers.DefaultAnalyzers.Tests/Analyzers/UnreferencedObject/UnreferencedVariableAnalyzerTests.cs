using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UnreferencedObject;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.UnreferencedObject;

public sealed class UnreferencedVariableAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<UnreferencedVariableAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenVariableIsReferenced_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            DECLARE @Var1 INT = 303
                            PRINT @Var1
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenVariableIsNotReferenced_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            DECLARE █AJ5012░main.sql░░@Var1███@Var1 INT = 303█
                            PRINT 'Hello'
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenSameVariableIsDefinedInDifferentBatches1_ThenTreatEveryBatchSeparately()
    {
        const string code = """
                            USE MyDb
                            GO
                            -- Variable not referenced in this batch
                            DECLARE █AJ5012░main.sql░░@Var1███@Var1 INT = 303█
                            PRINT 'Hello'
                            GO

                            -- Variable referenced in this batch
                            DECLARE @Var1 INT = 303
                            PRINT @Var1
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenSameVariableIsDefinedInDifferentBatches2_ThenTreatEveryBatchSeparately()
    {
        const string code = """
                            USE MyDb
                            GO
                            -- Variable referenced in this batch
                            DECLARE @Var1 INT = 303
                            PRINT @Var1

                            GO

                            -- Variable not referenced in this batch
                            DECLARE █AJ5012░main.sql░░@Var1███@Var1 INT = 303█
                            PRINT 'Hello'
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenParameterNotUsed_ThenOk_BecauseItIsNotVariable()
    {
        const string code = """
                            USE MyDb
                            GO
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
