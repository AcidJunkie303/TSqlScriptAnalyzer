using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;
using Xunit.Abstractions;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("maintainability", "CA1515:Consider making public types internal", Justification = "False positive. It is used in the DatabaseAnalyzers.DefaultAnalyzers.Tests project")]
public abstract class ScriptAnalyzerTestsBase<TAnalyzer>
    where TAnalyzer : class, IScriptAnalyzer, new()
{
    protected ScriptAnalyzerTestsBase(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
    }

    protected ITestOutputHelper TestOutputHelper { get; }

    protected static ScriptAnalyzerTesterBuilder<TAnalyzer> GetDefaultTesterBuilder(string sql)
        => ScriptAnalyzerTesterBuilder
            .Create<TAnalyzer>()
            .WithMainScriptFile(sql);

    protected void Verify(ScriptAnalyzerTester tester)
    {
        var syntaxTree = SyntaxTreeVisualizer.Visualize(tester.MainScript.Script);
        TestOutputHelper.WriteLine(syntaxTree);
        tester.Test();
    }

    protected void Verify(string sql) => Verify(GetDefaultTesterBuilder(sql).Build());

    protected void Verify<TSettings>(string sql, TSettings settings)
        where TSettings : class, ISettings<TSettings>
    {
        var tester = GetDefaultTesterBuilder(sql)
            .WithSettings(settings)
            .Build();

        Verify(tester);
    }

    protected void VerifyWithDefaultSettings<TSettings>(string sql)
        where TSettings : class, ISettings<TSettings>
        => Verify(sql, TSettings.Default);
}
