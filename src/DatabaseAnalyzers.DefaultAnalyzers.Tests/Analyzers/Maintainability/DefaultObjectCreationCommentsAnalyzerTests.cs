using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Maintainability;

public sealed class DefaultObjectCreationCommentsAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<DefaultObjectCreationCommentsAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenNoDefaultObjectCreationComments_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            /* whatever */
                            -- whatever 2

                            """;
        Verify(code);
    }

    [Fact]
    public void WhenDefaultObjectCreationComments_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            ▶️AJ5047💛script_0.sql💛✅/****** Object:  Table [dbo].[Table1]    Script Date: 2025-01-17 17:54:30 ******/◀️

                            """;
        Verify(code);
    }
}
