using System.Diagnostics.CodeAnalysis;
using System.Text;
using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using FluentAssertions;
using Microsoft.SqlServer.TransactSql.ScriptDom;
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
    [InlineData(KeywordNamingPolicy.UpperCase, "▶️AJ5048💛script_0.sql💛💛ChEcKpOiNt💛CHECKPOINT💛UpperCase✅ChEcKpOiNt◀️;")]
    [InlineData(KeywordNamingPolicy.LowerCase, "▶️AJ5048💛script_0.sql💛💛ChEcKpOiNt💛checkpoint💛LowerCase✅ChEcKpOiNt◀️;")]
    [InlineData(KeywordNamingPolicy.CamelCase, "▶️AJ5048💛script_0.sql💛💛ChEcKpOiNt💛checkPoint💛CamelCase✅ChEcKpOiNt◀️;")]
    [InlineData(KeywordNamingPolicy.PascalCase, "▶️AJ5048💛script_0.sql💛💛ChEcKpOiNt💛CheckPoint💛PascalCase✅ChEcKpOiNt◀️;")]
    public void Theory(object policy, string code)
    {
        var settings = new Aj5048Settings((KeywordNamingPolicy) policy);

        Verify(settings, code);
    }

    [Fact]
    [SuppressMessage("Globalization", "CA1305:Specify IFormatProvider")]
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
    [SuppressMessage("Minor Code Smell", "S4040:Strings should be normalized to uppercase")]
    public void GiveAllValuesOfTSqlTokenType()
    {
        var values = Enum
            .GetValues<TSqlTokenType>()
            .Select(a => a.ToString())
            .Order(StringComparer.Ordinal);

        var buffer = new StringBuilder(10000);
        foreach (var value in values)
        {
            buffer
                .AppendLine($"        {{ (TSqlTokenType.{value}, KeywordNamingPolicy.LowerCase), \"{value.ToLowerInvariant()}\" }},")
                .AppendLine($"        {{ (TSqlTokenType.{value}, KeywordNamingPolicy.CamelCase), \"{MakeCamelCase(value)}\" }},")
                .AppendLine($"        {{ (TSqlTokenType.{value}, KeywordNamingPolicy.PascalCase), \"{value}\" }},")
                .AppendLine($"        {{ (TSqlTokenType.{value}, KeywordNamingPolicy.UpperCase), \"{value.ToUpperInvariant()}\" }},");
        }

        File.WriteAllText("C:\\temp\\t.cs", buffer.ToString());

        true.Should().BeTrue();

#pragma warning disable
        static string MakeCamelCase(string value)
            => value[0].ToString().ToLowerInvariant() + value.Substring(1);
#pragma warning restore
    }
}
