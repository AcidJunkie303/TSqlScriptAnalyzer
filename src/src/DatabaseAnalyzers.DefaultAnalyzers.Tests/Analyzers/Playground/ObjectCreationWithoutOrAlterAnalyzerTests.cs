using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Playground;

public sealed class PlaygroundTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<ObjectCreationWithoutOrAlterAnalyzer>(testOutputHelper)
{
    [Fact]
    public void PlaygroundTests1()
    {
        const string code = """
                            USE MyDB
                            GO

                            SELECT value
                            FROM STRING_SPLIT('string_to_split', 'delimiter');

                            SELECT value
                            from MY_VABLE

                            SELECT value
                            from dbo.MY_VABLE

                            SELECT a.value
                            from dbo.MY_VABLE


                            """;

        var tester = GetDefaultTesterBuilder(code).Build();
        Verify(tester);
    }
}
