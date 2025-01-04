using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

public sealed class StatementsMustBeginOnNewLineAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<StatementsMustBeginOnNewLineAnalyzer>(testOutputHelper)
{
    private static readonly Aj5023Settings DefaultSettings = new Aj5023SettingsRaw
    {
        StatementTypesToIgnore = ["goto", "print", "set"]
    }.ToSettings();

    [Fact]
    public void WhenAllStatementsAreOnSeparateLines_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF @x < 0
                                SET @x = 0;
                            """;
        Verify(DefaultSettings, code);
    }

    [Fact]
    public void WhenSelectOnSameLine_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (@x < 0) ▶️AJ5023💛script_0.sql💛✅SELECT 1◀️
                            """;
        Verify(DefaultSettings, code);
    }

    [Fact]
    public void WhenSetVariableValueOnSameLine_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (@x < 0) SET @x = 0
                            """;
        Verify(DefaultSettings, code);
    }

    [Fact]
    public void WhenIfOnSameLine_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (@x < 0) SET @x = 0; ▶️AJ5023💛script_0.sql💛✅IF @y < 0 SET @y = 0◀️
                            """;
        Verify(DefaultSettings, code);
    }

    [Fact]
    public void WhenSetVariableIsOnSameLine_WhenSetVariableNotIgnored_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (@x < 0) ▶️AJ5023💛script_0.sql💛✅SET @x = 0◀️
                            """;

        Verify(Aj5023Settings.Default, code);
    }
}
