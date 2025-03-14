using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

public sealed class MissingBeginEndAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<MissingBeginEndAnalyzer>(testOutputHelper)
{
    private static readonly Aj5022Settings NoBeginEndRequiredSettings = new(IfRequiresBeginEndBlock: false, WhileRequiresBeginEndBlock: false);
    private static readonly Aj5022Settings BeginEndRequiredSettings = new(IfRequiresBeginEndBlock: true, WhileRequiresBeginEndBlock: true);

    [Fact]
    public void WithIfElse_WithNoBeginEndRequired_WhenNotUsingBeginEnd_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1=1)
                                PRINT 'tb'
                            ELSE
                                PRINT '303'
                            """;

        Verify(NoBeginEndRequiredSettings, code);
    }

    [Fact]
    public void WithIfElse_WithNoBeginEndRequired_WhenUsingBeginEnd_ThenOk()
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

        Verify(NoBeginEndRequiredSettings, code);
    }

    [Fact]
    public void WithWhile_WithNoBeginEndRequired_WhenUsingBeginEnd_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            WHILE (1=1)
                            BEGIN
                                PRINT 'tb'
                            END
                            """;
        Verify(NoBeginEndRequiredSettings, code);
    }

    [Fact]
    public void WithWhile_WithNoBeginEndRequired_WhenNotUsingBeginEndThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            WHILE (1=1)
                                PRINT 'tb-303'
                            """;
        Verify(NoBeginEndRequiredSettings, code);
    }

    [Fact]
    public void WithIfElse_WithBeginEndRequired_WhenNotUsingBeginEnd_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1=1)
                                ▶️AJ5022💛script_0.sql💛💛IF✅PRINT 'tb'◀️
                            ELSE
                                ▶️AJ5022💛script_0.sql💛💛ELSE✅PRINT '303'◀️
                            """;
        Verify(BeginEndRequiredSettings, code);
    }

    [Fact]
    public void WithWhile_WithBeginEndRequired_WhenUsingBeginEnd_ThenOk()
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
        Verify(BeginEndRequiredSettings, code);
    }

    [Fact]
    public void WithWhile_WithBeginEndRequired_WhenNotUsingBeginEnd_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            WHILE (1=1)
                                ▶️AJ5022💛script_0.sql💛💛WHILE✅PRINT 'tb-303'◀️
                            """;
        Verify(BeginEndRequiredSettings, code);
    }

    [Fact]
    public void WithIfElse_WithBeginEndRequired_WhenUsingBeginEnd_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            WHILE (1=1)
                            BEGIN
                                PRINT 'tb-303'
                            END
                            """;
        Verify(BeginEndRequiredSettings, code);
    }
}
