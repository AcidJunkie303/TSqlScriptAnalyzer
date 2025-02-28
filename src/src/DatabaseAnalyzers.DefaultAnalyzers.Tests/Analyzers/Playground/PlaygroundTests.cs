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
                            DECLARE @name varchar(100)
                            SELECT @name

                            """;

        var tester = GetDefaultTesterBuilder(code).Build();
        Verify(tester);
    }
}
