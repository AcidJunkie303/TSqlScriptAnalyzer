using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;
using Xunit.Abstractions;

#pragma warning disable

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Runtime;

public sealed class MissingOrderByWhenSelectTopAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<MissingOrderByWhenSelectTopAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenNotUsingTop_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      ID
                            FROM        Table1
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenUsingTopAndOrderBy_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      TOP 1 ID
                            FROM        Table1
                            ORDER BY    Id
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenUsingTopWithoutOrderBy_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            █AJ5043░script_0.sql░███SELECT TOP 1 ID
                            FROM        Table1█
                            """;

        Verify(code);
    }
}
