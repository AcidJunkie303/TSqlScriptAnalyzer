using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

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

                            SELECT IIF(@a=1, 'Hello', ▶️AJ5033💛script_0.sql💛✅IIF(@b=1, 'world','there')◀️)
                            """;
        Verify(code);
    }
}
