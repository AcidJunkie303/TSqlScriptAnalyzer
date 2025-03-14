using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

public sealed class FileMustEndWithEmptyLineAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<FileMustEndWithEmptyLineAnalyzer>(testOutputHelper)
{
    [Theory]
    [InlineData("\r\n")]
    [InlineData("\n")]
    [InlineData("▶️AJ5005💛script_0.sql💛✅ ◀️")]
    public void Theory(string scriptEnding)
    {
        var code = $"PRINT 303{scriptEnding}";

        Verify(code);
    }
}
