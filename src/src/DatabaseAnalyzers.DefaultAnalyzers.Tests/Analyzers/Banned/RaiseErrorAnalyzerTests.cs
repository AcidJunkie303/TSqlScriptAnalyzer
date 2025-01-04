using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Banned;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Banned;

public sealed class RaiseErrorAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<RaiseErrorAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenNotUsingRaiseError_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT 1
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenUsingRaiseError_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            ▶️AJ5042💛script_0.sql💛✅RAISERROR (50005, 10, 1, N'Hello');◀️
                            """;

        Verify(code);
    }
}
