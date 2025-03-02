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
    [InlineData(KeywordNamingPolicy.UpperCase, "CHECKPOINT;")]
    [InlineData(KeywordNamingPolicy.LowerCase, "checkpoint;")]
    [InlineData(KeywordNamingPolicy.CamelCase, "checkPoint;")]
    [InlineData(KeywordNamingPolicy.PascalCase, "CheckPoint;")]
    [InlineData(KeywordNamingPolicy.UpperCase, "▶️AJ5056💛script_0.sql💛💛ChEcKpOiNt💛CHECKPOINT💛UpperCase✅ChEcKpOiNt◀️;")]
    [InlineData(KeywordNamingPolicy.LowerCase, "▶️AJ5056💛script_0.sql💛💛ChEcKpOiNt💛checkpoint💛LowerCase✅ChEcKpOiNt◀️;")]
    [InlineData(KeywordNamingPolicy.CamelCase, "▶️AJ5056💛script_0.sql💛💛ChEcKpOiNt💛checkPoint💛CamelCase✅ChEcKpOiNt◀️;")]
    [InlineData(KeywordNamingPolicy.PascalCase, "▶️AJ5056💛script_0.sql💛💛ChEcKpOiNt💛CheckPoint💛PascalCase✅ChEcKpOiNt◀️;")]
    public void Theory(object policy, string code)
    {
        var settings = new Aj5056Settings((KeywordNamingPolicy) policy);

        Verify(settings, code);
    }
}
