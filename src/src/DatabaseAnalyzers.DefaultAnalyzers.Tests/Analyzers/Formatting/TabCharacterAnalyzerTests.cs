using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

public sealed class TabCharacterAnalyzerTests(ITestOutputHelper testOutputHelper) : ScriptAnalyzerTestsBase<TabCharacterAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenNoTabFound_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            PRINT 303
                            """;

        Verify(code);
    }

    [Fact]
    [SuppressMessage("Minor Code Smell", "S105:Tabulation characters should not be used", Justification = "Using a tabulator character is part of the test")]
    public void WhenTabFound_ThenDiagnose()
    {
        // had to be done this way because the IDE replaces tabs with spaces...
        const string code = """
                            USE MyDb
                            GO
                            PRINT▶️AJ5008💛script_0.sql💛✅	◀️909 -- code is a tab character
                            """;
        Verify(code);
    }
}
