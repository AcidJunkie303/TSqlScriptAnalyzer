using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;
using DatabaseAnalyzer.Testing.Visualization;
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
        AstAndTokenVisualizer.Visualize(TestOutputHelper, tester.Scripts);

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
