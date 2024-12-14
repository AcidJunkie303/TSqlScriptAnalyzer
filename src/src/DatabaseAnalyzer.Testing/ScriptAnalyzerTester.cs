using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;
using FluentAssertions;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("maintainability", "CA1515:Consider making public types internal", Justification = "False positive. It is used in the DatabaseAnalyzers.DefaultAnalyzers.Tests project")]
public sealed class ScriptAnalyzerTester
{
    private readonly IAnalysisContext _analysisContext;
    private readonly IScriptAnalyzer _analyzer;

    public IScriptModel MainScript { get; }
    public IReadOnlyList<IIssue> ExpectedIssues { get; }

    public ScriptAnalyzerTester(
        IAnalysisContext analysisContext,
        IScriptAnalyzer analyzer,
        IScriptModel mainScript,
        IReadOnlyList<IIssue> expectedIssues)
    {
        _analysisContext = analysisContext;
        _analyzer = analyzer;
        MainScript = mainScript;
        ExpectedIssues = expectedIssues;
    }

    public void Test()
    {
        var firstScriptError = _analysisContext.Scripts.SelectMany(script => script.Errors).FirstOrDefault();
        if (firstScriptError is not null)
        {
            throw new InvalidOperationException($"Error in script: {firstScriptError}");
        }

        _analyzer.AnalyzeScript(_analysisContext, MainScript);

        var reportedIssues = _analysisContext.IssueReporter.GetIssues();
        reportedIssues.Should().BeEquivalentTo(ExpectedIssues);
    }
}
