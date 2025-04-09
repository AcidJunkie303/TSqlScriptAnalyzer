// TODO: remove when on .NET 9.0 only

#if NET8_0
using System.Collections.Frozen;
#endif

using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;
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

    [Fact]
    public void WhenCteWithSemiColon_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            ; WITH MyCTE AS
                            (
                                SELECT 2
                            )
                            SELECT * FROM MyCTE
                            """;
// TODO: remove when on .NET 9.0 only
#if NET8_0
        var settings = new Aj5023Settings(new[] { TSqlTokenType.Semicolon }.ToFrozenSet());
#else
        var settings = new Aj5023Settings([TSqlTokenType.Semicolon]);
#endif
        Verify(settings, code);
    }

    [Fact]
    public void WhenIfElseIf_WithoutBeginEnd_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1=1)
                                PRINT 'tb'
                            ELSE IF (2=2)
                                PRINT '303'
                            """;

        Verify(Aj5023Settings.Default, code);
    }

    [Fact]
    public void WhenIfElseIf_WithBeginEnd_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1=1)
                            BEGIN
                                PRINT 'tb'
                            END
                            ELSE IF (2=2)
                            BEGIN
                                PRINT '303'
                            END
                            """;

        Verify(Aj5023Settings.Default, code);
    }
}
