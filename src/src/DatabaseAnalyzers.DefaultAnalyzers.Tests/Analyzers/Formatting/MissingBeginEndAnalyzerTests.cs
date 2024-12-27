using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

public sealed class MissingBeginEndAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<MissingBeginEndAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenUsingIfElseWithBeginEnd_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1=1)
                            BEGIN
                                PRINT 'tb'
                            END
                            ELSE
                            BEGIN
                                PRINT '303'
                            END
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenUsingIfElseWithoutBeginEnd_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1=1)
                                █AJ5022░script_0.sql░░IF███PRINT 'tb'█
                            ELSE
                                █AJ5022░script_0.sql░░ELSE███PRINT '303'█
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenUsingWhileWithBeginEnd_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            WHILE (1=1)
                            BEGIN
                                PRINT 'tb-303'
                            END
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenUsingWhileWithoutBeginEnd_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            WHILE (1=1)
                                █AJ5022░script_0.sql░░WHILE███PRINT 'tb-303'█
                            """;
        Verify(code);
    }
}
