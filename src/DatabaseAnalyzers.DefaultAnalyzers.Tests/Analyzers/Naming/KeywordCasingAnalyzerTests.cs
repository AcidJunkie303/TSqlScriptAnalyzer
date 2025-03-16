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
    [InlineData(Aj5056SKeywordNamingPolicy.UpperCase, "PRINT 99999999999999999999999999.99")]
    [InlineData(Aj5056SKeywordNamingPolicy.UpperCase, "CHECKPOINT;")]
    [InlineData(Aj5056SKeywordNamingPolicy.LowerCase, "checkpoint;")]
    [InlineData(Aj5056SKeywordNamingPolicy.CamelCase, "checkPoint;")]
    [InlineData(Aj5056SKeywordNamingPolicy.PascalCase, "CheckPoint;")]
    [InlineData(Aj5056SKeywordNamingPolicy.UpperCase, "▶️AJ5056💛script_0.sql💛💛ChEcKpOiNt💛CHECKPOINT💛UpperCase✅ChEcKpOiNt◀️;")]
    [InlineData(Aj5056SKeywordNamingPolicy.LowerCase, "▶️AJ5056💛script_0.sql💛💛ChEcKpOiNt💛checkpoint💛LowerCase✅ChEcKpOiNt◀️;")]
    [InlineData(Aj5056SKeywordNamingPolicy.CamelCase, "▶️AJ5056💛script_0.sql💛💛ChEcKpOiNt💛checkPoint💛CamelCase✅ChEcKpOiNt◀️;")]
    [InlineData(Aj5056SKeywordNamingPolicy.PascalCase, "▶️AJ5056💛script_0.sql💛💛ChEcKpOiNt💛CheckPoint💛PascalCase✅ChEcKpOiNt◀️;")]
    [InlineData(Aj5056SKeywordNamingPolicy.UpperCase, "SET ▶️AJ5056💛script_0.sql💛💛identity_insert💛IDENTITY_INSERT💛UpperCase✅identity_insert◀️ Table1 ON")]
    public void Theory(object policy, string code)
    {
        var settings = new Aj5056Settings((Aj5056SKeywordNamingPolicy) policy);

        Verify(settings, code);
    }
}
