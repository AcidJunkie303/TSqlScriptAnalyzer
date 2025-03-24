using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

public sealed class MissingEmptyLineAroundGoStatementAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<MissingEmptyLineAroundGoStatementAnalyzer>(testOutputHelper)
{
    private static readonly Aj5045Settings RequiredBeforeSettings = new(RequireEmptyLineBeforeGo: true, RequireEmptyLineAfterGo: false);
    private static readonly Aj5045Settings RequiredAfterSettings = new(RequireEmptyLineBeforeGo: false, RequireEmptyLineAfterGo: true);

    [Fact]
    public void WithDefaultSettings_WhenNoEmptyLinesAfterAndBefore_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO
                            PRINT 303
                            """;

        Verify(Aj5045Settings.Default, code);
    }

    [Fact]
    public void WithRequiredNewLineBefore_WhenEmptyLineBefore_ThenOk()
    {
        const string code = """
                            USE MyDb

                            GO
                            PRINT 303
                            """;

        Verify(RequiredBeforeSettings, code);
    }

    [Fact]
    public void WithRequiredNewLineBefore_WhenNoEmptyLineBefore_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            ▶️AJ5045💛script_0.sql💛💛before✅GO◀️
                            PRINT 303
                            """;

        Verify(RequiredBeforeSettings, code);
    }

    [Fact]
    public void WithRequiredNewLineAfter_WhenEmptyLineAfter_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            PRINT 303
                            """;

        Verify(RequiredAfterSettings, code);
    }

    [Fact]
    public void WithRequiredNewLineAfter_WhenNoEmptyLineAfter_ThenDiagnose()
    {
        const string code = """
                            USE MyDb

                            ▶️AJ5045💛script_0.sql💛💛after✅GO◀️
                            PRINT 303
                            """;

        Verify(RequiredAfterSettings, code);
    }

    [Fact]
    public void WithRequiredNewLineAfter_WhenJustCommentLineAfter_ThenDiagnose()
    {
        const string code = """
                            USE MyDb

                            ▶️AJ5045💛script_0.sql💛💛after✅GO◀️
                            -- some comments
                            PRINT 303
                            """;

        Verify(RequiredAfterSettings, code);
    }
}
