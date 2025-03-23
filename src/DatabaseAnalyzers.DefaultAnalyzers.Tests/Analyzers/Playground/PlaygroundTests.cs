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

                            PRINT 303

                            """;

        var tester = GetDefaultTesterBuilder(code).Build();
        Verify(tester);
    }

    [Fact]
    public void PlaygroundTests2()
    {
        const string code = """
                            USE MyDB
                            GO

                            CREATE OR ALTER VIEW dbo.V1
                            AS
                                SELECT
                                    1 AS Expr1,
                                    Column1,
                                    Column2 AS MyColumn
                                FROM Table1

                            """;

        var tester = GetDefaultTesterBuilder(code).Build();
        Verify(tester);
    }
}
