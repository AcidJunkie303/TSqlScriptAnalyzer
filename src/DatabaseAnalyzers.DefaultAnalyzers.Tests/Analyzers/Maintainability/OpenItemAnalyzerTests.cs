using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Maintainability;

public sealed class OpenItemAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<OpenItemAnalyzer>(testOutputHelper)
{
    private static readonly Aj5004Settings Settings = new Aj5004SettingsRaw
    {
        TopicsAndPatterns =
        [
            new TopicAndPatternRaw
            {
                Topic = "to do",
                Pattern = """TODO\s*:\s*(?<message>[^\r\n]+)"""
            },
            new TopicAndPatternRaw
            {
                Topic = "open point",
                Pattern = """\{OpenPoint\}(?<message>[^\r\n]+)\{OpenPoint\}"""
            }
        ]
    }.ToSettings();

    [Fact]
    public void WhenNoMarkersInComment_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            -- bla
                            /* bla */

                            """;
        Verify(Settings, code);
    }

    [Fact]
    public void WhenMarkerInEndOfLineComment_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            ▶️AJ5004💛script_0.sql💛💛to do💛not complete✅-- TODO: not complete◀️
                            """;
        Verify(Settings, code);
    }

    [Fact]
    public void WhenMarkerInMultiLineComment_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            ▶️AJ5004💛script_0.sql💛💛open point💛not finished yet✅/*

                            {OpenPoint}not finished yet{OpenPoint}

                            *◀️/
                            """;
        Verify(Settings, code);
    }
}
