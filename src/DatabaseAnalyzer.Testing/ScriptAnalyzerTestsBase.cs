using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;
using Xunit.Abstractions;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("maintainability", "CA1515:Consider making public types internal", Justification = "False positive. It is used in the DatabaseAnalyzers.DefaultAnalyzers.Tests project")]
public abstract class ScriptAnalyzerTestsBase<TAnalyzer>
    where TAnalyzer : class, IScriptAnalyzer
{
    protected ITestOutputHelper TestOutputHelper { get; }

    protected ScriptAnalyzerTestsBase(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
    }

    protected static ScriptAnalyzerTesterBuilder<TAnalyzer> GetDefaultTesterBuilder(string sql)
        => ScriptAnalyzerTesterBuilder
            .Create<TAnalyzer>()
            .WithScriptFile(sql, "MyDb");

    protected void Verify(ScriptAnalyzerTester tester)
    {
        TestOutputHelper.WriteLine(string.Empty);
        TestOutputHelper.WriteLine(string.Empty);
        TestOutputHelper.WriteLine(string.Empty);
        TestOutputHelper.WriteLine("Syntax Tree:");
        TestOutputHelper.WriteLine(SyntaxTreeVisualizer.Visualize(tester.MainScript.ParsedScript));

        TestOutputHelper.WriteLine(string.Empty);
        TestOutputHelper.WriteLine(string.Empty);
        TestOutputHelper.WriteLine(string.Empty);
        TestOutputHelper.WriteLine("Tokens:");
        TestOutputHelper.WriteLine(TokenVisualizer.Visualize(tester.MainScript.ParsedScript));

        tester.Test();
    }

    protected void Verify(string sql) => Verify(GetDefaultTesterBuilder(sql).Build());

    protected void Verify(params string[] scripts)
    {
        if (scripts.Length == 0)
        {
            throw new ArgumentException("At least one script is required", nameof(scripts));
        }

        var builder = GetDefaultTesterBuilder(scripts[0]);
        foreach (var scriptContent in scripts.Skip(1))
        {
            builder.WithScriptFile(scriptContent, "MyDb");
        }

        var tester = builder.Build();
        Verify(tester);
    }

    protected void Verify<TSettings>(TSettings settings, params string[] scripts)
        where TSettings : class, ISettings<TSettings>
    {
        if (scripts.Length == 0)
        {
            throw new ArgumentException("At least one script is required", nameof(scripts));
        }

        var builder = GetDefaultTesterBuilder(scripts[0]);
        foreach (var scriptContent in scripts.Skip(1))
        {
            builder.WithScriptFile(scriptContent, "MyDb");
        }

        var tester = builder
            .WithSettings(settings)
            .WithTestOutputHelper(TestOutputHelper)
            .Build();

        Verify(tester);
    }

    protected void VerifyWithDefaultSettings<TSettings>(params string[] scripts)
        where TSettings : class, ISettings<TSettings>
        => Verify(TSettings.Default, scripts);
}
