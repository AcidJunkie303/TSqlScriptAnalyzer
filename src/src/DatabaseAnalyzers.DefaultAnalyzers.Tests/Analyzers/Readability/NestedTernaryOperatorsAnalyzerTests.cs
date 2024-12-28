using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Readability;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Readability;

public sealed class NestedTernaryOperatorsAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<NestedTernaryOperatorsAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenNoNestedTernaryOperator_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT IIF(@a=1, 'Hello', 'world')
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenNestedTernaryOperator_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT IIF(@a=1, 'Hello', █AJ5033░script_0.sql░███IIF(@b=1, 'world','there')█)
                            """;
        Verify(code);
    }
}
