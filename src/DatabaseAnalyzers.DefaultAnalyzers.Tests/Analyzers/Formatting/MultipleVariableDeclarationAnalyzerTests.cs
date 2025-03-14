using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

public sealed class MultipleVariableDeclarationAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<MultipleVariableDeclarationAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenEachVariableIsDeclaredSeparately_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            DECLARE @a INT
                            DECLARE @b INT
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenEachVariableIsDeclaredInMultiDeclaration_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            ▶️AJ5024💛script_0.sql💛✅DECLARE @a INT, @b INT◀️

                            PRINT 909
                            """;
        Verify(code);
    }
}
