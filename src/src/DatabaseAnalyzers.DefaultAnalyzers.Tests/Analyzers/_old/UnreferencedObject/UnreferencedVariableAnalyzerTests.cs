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
        const string sql = """
                           DECLARE @Var1 INT = 303
                           PRINT @Var1
                           """;
        Verify(sql);
    }

    [Fact]
    public void WhenVariableIsNotReferenced_ThenDiagnose()
    {
        const string sql = """
                           DECLARE █AJ5012░main.sql░░@Var1███@Var1█ INT = 303
                           PRINT 'Hello'
                           """;
        Verify(sql);
    }

    [Fact]
    public void WhenSameVariableIsDefinedInDifferentBatches1_ThenTreatEveryBatchSeparately()
    {
        const string sql = """
                           -- Variable not referenced in this batch
                           DECLARE █AJ5012░main.sql░░@Var1███@Var1█ INT = 303
                           PRINT 'Hello'
                           GO

                           -- Variable referenced in this batch
                           DECLARE @Var1 INT = 303
                           PRINT @Var1

                           """;
        Verify(sql);
    }

    [Fact]
    public void WhenSameVariableIsDefinedInDifferentBatches2_ThenTreatEveryBatchSeparately()
    {
        const string sql = """
                           -- Variable referenced in this batch
                           DECLARE @Var1 INT = 303
                           PRINT @Var1

                           GO

                           -- Variable not referenced in this batch
                           DECLARE █AJ5012░main.sql░░@Var1███@Var1█ INT = 303
                           PRINT 'Hello'
                           """;
        Verify(sql);
    }

    [Fact]
    public void WhenParameterNotUsed_ThenOk_BecauseItIsNotVariable()
    {
        const string sql = """
                           CREATE PROCEDURE P1
                              @Param1 INT
                           AS
                           BEGIN
                               SELECT 1
                           END

                           """;
        Verify(sql);
    }
}
