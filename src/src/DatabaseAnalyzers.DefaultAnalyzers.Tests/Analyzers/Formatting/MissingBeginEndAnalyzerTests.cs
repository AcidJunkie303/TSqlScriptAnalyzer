using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

public sealed class MissingBeginEndAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<MissingBeginEndAnalyzer>(testOutputHelper)
{
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

        Verify(Settings.NoBeginEndRequired, code);
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

        Verify(Settings.NoBeginEndRequired, code);
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
        Verify(Settings.NoBeginEndRequired, code);
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
        Verify(Settings.NoBeginEndRequired, code);
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
        Verify(Settings.BeginEndRequired, code);
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
        Verify(Settings.BeginEndRequired, code);
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
        Verify(Settings.BeginEndRequired, code);
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
        Verify(Settings.BeginEndRequired, code);
    }

    private static class Settings
    {
        public static Aj5022Settings NoBeginEndRequired { get; } = new(IfRequiresBeginEndBlock: false, WhileRequiresBeginEndBlock: false);
        public static Aj5022Settings BeginEndRequired { get; } = new(IfRequiresBeginEndBlock: true, WhileRequiresBeginEndBlock: true);
    }
}
