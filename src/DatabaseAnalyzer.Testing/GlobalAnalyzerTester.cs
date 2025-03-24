using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;
using FluentAssertions;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("maintainability", "CA1515:Consider making public types internal", Justification = "False positive. It is used in the DatabaseAnalyzers.DefaultAnalyzers.Tests project")]
public sealed class GlobalAnalyzerTester
{
    private readonly IGlobalAnalysisContext _analysisContext;
    private readonly IGlobalAnalyzer _analyzer;

    public IReadOnlyList<IScriptModel> Scripts { get; }
    public IReadOnlyList<IIssue> ExpectedIssues { get; }

    public GlobalAnalyzerTester(
        IGlobalAnalysisContext analysisContext,
        IGlobalAnalyzer analyzer,
        IReadOnlyList<IScriptModel> scripts,
        IReadOnlyList<IIssue> expectedIssues)
    {
        _analysisContext = analysisContext;
        _analyzer = analyzer;
        Scripts = scripts;
        ExpectedIssues = expectedIssues;
    }

    public void Test()
    {
        var firstScriptError = _analysisContext.Scripts.SelectMany(static script => script.Errors).FirstOrDefault();
        if (firstScriptError is not null)
        {
            throw new InvalidOperationException($"Error in script: {firstScriptError}");
        }

        _analyzer.Analyze();

        var reportedIssues = _analysisContext.IssueReporter.Issues;

        // sometimes the order is not the same, therefore we cannot use BeEquivalentTo()
        reportedIssues.Should().HaveCount(ExpectedIssues.Count);
        foreach (var expectedIssue in ExpectedIssues)
        {
            reportedIssues.Should().ContainEquivalentOf(expectedIssue, static options => options.Excluding(static x => x.DatabaseName));
        }
    }
}
