using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Naming;

public sealed class AliasReferenceWithDifferentCasingAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<AliasReferenceWithDifferentCasingAnalyzer>(testOutputHelper)
{
    [Theory]
    [InlineData("t1")]
    [InlineData("▶️AJ5065💛script_0.sql💛💛T1💛t1✅T1◀️")]
    public void Theory(string aliasReferenceName)
    {
        var code = $"""
                    USE MyDb
                    GO

                    SELECT      t1.Id
                    FROM        Table1 t1
                    INNER JOIN  Table2 t2 ON t2.Id = {aliasReferenceName}.Id
                    """;
        Verify(code);
    }
}
