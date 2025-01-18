using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Naming;

[SuppressMessage("Major Code Smell", "S4144:Methods should not have identical implementations", Justification = "Different test data")]
public sealed class KeywordCasingAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<KeywordCasingAnalyzer>(testOutputHelper)
{
    [Theory]
    [InlineData(KeywordNamingPolicy.UpperCase, "PRINT 303")]
    [InlineData(KeywordNamingPolicy.CamelCase, "▶️AJ5048💛script_0.sql💛💛PRINT💛print💛CamelCase✅PRINT◀️ 303")]
    [InlineData(KeywordNamingPolicy.PascalCase, "▶️AJ5048💛script_0.sql💛💛PRINT💛Print💛PascalCase✅PRINT◀️ 303")]
    public void SingleWordTheory(object policy, string code)
    {
        var settings = new Aj5048SettingsRaw
        {
            KeywordNamingPolicy = (KeywordNamingPolicy) policy,
            ExcludedKeywordTokenTypes = ["Identifier"]
        }.ToSettings();

        Verify(settings, code);
    }

    [Theory]
    [InlineData(KeywordNamingPolicy.UpperCase, "WAITFOR DELAY '00:00:10'")]
    [InlineData(KeywordNamingPolicy.CamelCase, "▶️AJ5048💛script_0.sql💛💛WAITFOR💛waitFor💛CamelCase✅WAITFOR◀️ DELAY '00:00:10'")]
    [InlineData(KeywordNamingPolicy.PascalCase, "▶️AJ5048💛script_0.sql💛💛WAITFOR💛WaitFor💛PascalCase✅WAITFOR◀️ DELAY '00:00:10'")]
    public void MultiWordTheory(object policy, string code)
    {
        var settings = new Aj5048SettingsRaw
        {
            KeywordNamingPolicy = (KeywordNamingPolicy) policy,
            ExcludedKeywordTokenTypes = ["Identifier"]
        }.ToSettings();

        Verify(settings, code);
    }
}
