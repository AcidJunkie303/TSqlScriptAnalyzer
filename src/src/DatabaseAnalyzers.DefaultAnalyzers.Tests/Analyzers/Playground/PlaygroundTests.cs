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

                            select TOP 1 ID
                            from Table1
                            ORDER BY Id

                            update TOP (1) t1
                            SET Value1 = 123
                            FROM Table1 t1

                            """;

        var tester = GetDefaultTesterBuilder(code).Build();
        Verify(tester);
    }
}
