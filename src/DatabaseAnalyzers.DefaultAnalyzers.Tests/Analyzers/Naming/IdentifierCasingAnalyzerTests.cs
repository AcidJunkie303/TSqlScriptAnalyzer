using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Naming;

[SuppressMessage("Major Code Smell", "S4144:Methods should not have identical implementations", Justification = "Different test data")]
public sealed class IdentifierCasingAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<IdentifierCasingAnalyzer>(testOutputHelper)
{
    [Theory]
    [InlineData("INT")]
    [InlineData("int")]
    [InlineData("▶️AJ5057💛script_0.sql💛💛NvArChAr💛NVARCHAR✅NvArChAr◀️")]
    public void Theory(string code)
    {
        var settings = new Aj5057SettingsRaw
        {
            CasingByIdentifier = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                { "NVarChar", "NVARCHAR" }
            }
        }.ToSettings();

        Verify(settings, code);
    }
}
