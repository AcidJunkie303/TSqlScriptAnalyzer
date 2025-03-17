using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;
using Xunit.Abstractions;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("maintainability", "CA1515:Consider making public types internal", Justification = "False positive. It is used in the DatabaseAnalyzers.DefaultAnalyzers.Tests project")]
public abstract class GlobalAnalyzerTestsBase<TAnalyzer>
    where TAnalyzer : class, IGlobalAnalyzer
{
    protected ITestOutputHelper TestOutputHelper { get; }

    protected GlobalAnalyzerTestsBase(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
    }

    protected static GlobalAnalyzerTesterBuilder<TAnalyzer> GetDefaultTesterBuilder(params string[] scriptsContents)
    {
        var builder = GlobalAnalyzerTesterBuilder
            .Create<TAnalyzer>();

        foreach (var scriptContent in scriptsContents)
        {
            builder.WithScriptFile(scriptContent, "MyDb");
        }

        return builder;
    }

    protected void Verify(GlobalAnalyzerTester tester)
    {
        foreach (var script in tester.Scripts)
        {
            TestOutputHelper.WriteLine("***************************************************************************");
            TestOutputHelper.WriteLine(string.Empty);
            TestOutputHelper.WriteLine(string.Empty);
            TestOutputHelper.WriteLine(string.Empty);
            TestOutputHelper.WriteLine($"Syntax Tree of script {script.RelativeScriptFilePath}:");
            TestOutputHelper.WriteLine(SyntaxTreeVisualizer.Visualize(script.ParsedScript));

            TestOutputHelper.WriteLine(string.Empty);
            TestOutputHelper.WriteLine(string.Empty);
            TestOutputHelper.WriteLine(string.Empty);
            TestOutputHelper.WriteLine($"Tokens of script {script.RelativeScriptFilePath}:");
            TestOutputHelper.WriteLine(TokenVisualizer.Visualize(script.ParsedScript.ScriptTokenStream));
        }

        tester.Test();
    }

    protected void Verify(params string[] scripts)
    {
        if (scripts.Length == 0)
        {
            throw new ArgumentException("At least one script is required", nameof(scripts));
        }

        Verify(GetDefaultTesterBuilder(scripts).Build());
    }

    protected virtual void Verify<TSettings>(TSettings settings, params string[] scripts)
        where TSettings : class, IDiagnosticSettings<TSettings>
    {
        if (scripts.Length == 0)
        {
            throw new ArgumentException("At least one script is required", nameof(scripts));
        }

        var tester = GetDefaultTesterBuilder(scripts)
            .WithSettings(settings)
            .Build();

        Verify(tester);
    }

    protected void VerifyWithDefaultSettings<TSettings>(params string[] scripts)
        where TSettings : class, IDiagnosticSettings<TSettings>
        => Verify(TSettings.Default, scripts);
}
