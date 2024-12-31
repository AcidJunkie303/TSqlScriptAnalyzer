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

                            SELECT      t2.*
                            FROM (
                                SELECT      t1.C1, t1.C2
                                FROM        dbo.Table1 t1
                            ) AS t2

                            SELECT
                            t1.*,
                            t2.Value2
                            FROM Table1 t1
                            INNER JOIN Table2 t2 ON t2.Id = t1.Id

                            """;

        var tester = GetDefaultTesterBuilder(code).Build();
        Verify(tester);
    }
}
