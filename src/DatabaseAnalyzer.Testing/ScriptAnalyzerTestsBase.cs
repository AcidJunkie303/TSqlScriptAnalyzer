using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;
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

    protected static ScriptAnalyzerTesterBuilder<TAnalyzer> GetDefaultTesterBuilder(params string[] scriptsContents)
    {
        var builder = ScriptAnalyzerTesterBuilder
            .Create<TAnalyzer>();

        foreach (var scriptContent in scriptsContents)
        {
            builder.WithScriptFile(scriptContent, "MyDb");
        }

        return builder;
    }

    protected void Verify(ScriptAnalyzerTester tester)
    {
        foreach (var script in tester.AnalysisContext.Scripts)
        {
            TestOutputHelper.WriteLine(string.Empty);
            TestOutputHelper.WriteLine(string.Empty);
            TestOutputHelper.WriteLine(string.Empty);
            TestOutputHelper.WriteLine("==========================================");
            TestOutputHelper.WriteLine($"= Syntax Tree of script {script.RelativeScriptFilePath}");
            TestOutputHelper.WriteLine("==========================================");
            TestOutputHelper.WriteLine(SyntaxTreeVisualizer.Visualize(script.ParsedScript));

            TestOutputHelper.WriteLine(string.Empty);
            TestOutputHelper.WriteLine(string.Empty);
            TestOutputHelper.WriteLine(string.Empty);
            TestOutputHelper.WriteLine("==========================================");
            TestOutputHelper.WriteLine($"= Tokens of script {script.RelativeScriptFilePath}");
            TestOutputHelper.WriteLine("==========================================");
            TestOutputHelper.WriteLine(TokenVisualizer.Visualize(script.ParsedScript));
        }

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
        where TSettings : class, IDiagnosticSettings<TSettings>
    {
        if (scripts.Length == 0)
        {
            throw new ArgumentException("At least one script is required", nameof(scripts));
        }

        var tester = GetDefaultTesterBuilder(scripts)
            .WithSettings(settings)
            .WithTestOutputHelper(TestOutputHelper)
            .Build();

        Verify(tester);
    }

    protected void VerifyWithDefaultSettings<TSettings>(params string[] scripts)
        where TSettings : class, IDiagnosticSettings<TSettings>
        => Verify(TSettings.Default, scripts);
}
