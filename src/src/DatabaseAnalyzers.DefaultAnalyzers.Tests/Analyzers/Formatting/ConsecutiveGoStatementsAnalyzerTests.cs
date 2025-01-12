using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

public sealed class ConsecutiveGoStatementsAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<ConsecutiveGoStatementsAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenNoConsecutiveGoStatement_ThenOk()
    {
        const string code = """
                            PRINT 303
                            GO
                            """;

        Verify(Aj5045Settings.Default, code);
    }

    [Fact]
    public void WhenTwoConsecutiveGoStatement_ThenDiagnose()
    {
        const string code = """
                            ▶️AJ5046💛script_0.sql💛✅GO
                            GO◀️
                            PRINT 303
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenTwoConsecutiveGoStatement_SeparatedByMultiLineComments_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            ▶️AJ5046💛script_0.sql💛✅GO
                            /* comment */
                            -- comment
                            GO◀️
                            PRINT 303
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenTwoConsecutiveGoStatement_SeparatedOtherStatements_ThenOk()
    {
        const string code = """
                            GO
                            PRINT 303
                            GO
                            """;

        Verify(code);
    }
}
