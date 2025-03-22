using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Maintainability;

public sealed class DeadCodeAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<DeadCodeAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenNoCodeAfterReturn_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            PRINT 'Hello'
                            RETURN
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenCodeAfterReturn_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            PRINT 'Hello'
                            ▶️AJ5035💛script_0.sql💛💛RETURN✅RETURN◀️
                            PRINT 'Hello'
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenNoCodeAfterThrow_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            PRINT 'Hello'
                            THROW 60000, 'ooops', 1;
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenCodeAfterThrow_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            PRINT 'Hello'
                            ▶️AJ5035💛script_0.sql💛💛THROW✅THROW 60000, 'ooops', 1;◀️
                            PRINT 'Hello'
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenNoCodeAfterBreak_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            WHILE(1=1)
                            BEGIN
                                PRINT 'Hello'
                                BREAK
                            END

                            PRINT 'Hello'
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenNoCodeAfterBreak_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            WHILE(1=1)
                            BEGIN
                                PRINT 'Hello'
                                ▶️AJ5035💛script_0.sql💛💛BREAK✅BREAK◀️
                                PRINT 'Hello'
                            END

                            PRINT 'Hello'
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenNoCodeAfterContinue_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            WHILE(1=1)
                            BEGIN
                                PRINT 'Hello'
                                CONTINUE
                            END

                            PRINT 'Hello'
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenNoCodeAfterContinue_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            WHILE(1=1)
                            BEGIN
                                PRINT 'Hello'
                                ▶️AJ5035💛script_0.sql💛💛CONTINUE✅CONTINUE◀️
                                PRINT 'Hello'
                            END

                            PRINT 'Hello'
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenNoCodeAfterGoTo_WhenNoCodeBetweenGotoAndTargetLabel_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                                GOTO MyLabel
                            MyLabel:
                                PRINT 'Hello'

                            """;
        Verify(code);
    }

    [Fact]
    public void WhenNoCodeAfterGoTo_Case2_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            if (1=1)
                            BEGIN
                                GOTO MyLabel
                            END

                            PRINT 'Hello'

                            MyLabel:
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenCodeAfterGoTo_Case1_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            ▶️AJ5035💛script_0.sql💛💛GOTO✅GOTO MyLabel◀️
                            PRINT 303

                            MyLabel:
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenCodeAfterGoTo_Case2_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            if (1=1)
                            BEGIN
                                ▶️AJ5035💛script_0.sql💛💛GOTO✅GOTO MyLabel◀️
                                PRINT 303
                            END

                            MyLabel:
                            """;
        Verify(code);
    }
}
