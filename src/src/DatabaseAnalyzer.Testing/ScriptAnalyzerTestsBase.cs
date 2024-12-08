using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;
using Xunit.Abstractions;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("maintainability", "CA1515:Consider making public types internal", Justification = "False positive. It is used in the DatabaseAnalyzers.DefaultAnalyzers.Tests project")]
public abstract class ScriptAnalyzerTestsBase<TAnalyzer>
    where TAnalyzer : class, IScriptAnalyzer, new()
{
    protected ITestOutputHelper TestOutputHelper { get; }

    protected ScriptAnalyzerTestsBase(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
    }

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
}
