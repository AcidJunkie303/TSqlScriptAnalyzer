using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Banned;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Banned;

public sealed class BannedFunctionAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<BannedFunctionAnalyzer>(testOutputHelper)
{
    private static readonly Aj5040Settings Settings = new Aj5040SettingsRaw
    {
        BannedFunctionNamesByReason = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["LEN"] = "Reason1",
            ["My.BannedFunction"] = "Reason2"
        }
    }.ToSettings();

    [Fact]
    public void WhenBuiltInFunctionIsNotBanned_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT CONVERT(float, '123');

                            SELECT value
                            FROM STRING_SPLIT('Hello', 'e');
                            """;

        Verify(Settings, code);
    }

    [Fact]
    public void WhenBuiltInFunctionIsBanned_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT █AJ5040░script_0.sql░░LEN░Reason1███LEN█('Hello')
                            """;

        Verify(Settings, code);
    }

    [Fact]
    public void WhenSchemaBoundFunctionIsNotBanned_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT value FROM My.SimpleFunction(303)
                            """;

        Verify(Settings, code);
    }

    [Fact]
    public void WhenSchemaBoundFunctionIsBanned_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT value FROM █AJ5040░script_0.sql░░My.BannedFunction░Reason2███My.BannedFunction█(303)
                            """;

        Verify(Settings, code);
    }
}
