using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;
using FluentAssertions;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("maintainability", "CA1515:Consider making public types internal", Justification = "False positive. It is used in the DatabaseAnalyzers.DefaultAnalyzers.Tests project")]
public sealed class GlobalAnalyzerTester
{
    private readonly IAnalysisContext _analysisContext;
    private readonly IGlobalAnalyzer _analyzer;

    public GlobalAnalyzerTester(
        IAnalysisContext analysisContext,
        IGlobalAnalyzer analyzer,
        IReadOnlyList<IScriptModel> scripts,
        IReadOnlyList<IIssue> expectedIssues)
    {
        _analysisContext = analysisContext;
        _analyzer = analyzer;
        Scripts = scripts;
        ExpectedIssues = expectedIssues;
    }

    public IReadOnlyList<IScriptModel> Scripts { get; }
    public IReadOnlyList<IIssue> ExpectedIssues { get; }

    public void Test()
    {
        var firstScriptError = _analysisContext.Scripts.SelectMany(static script => script.Errors).FirstOrDefault();
        if (firstScriptError is not null)
        {
            throw new InvalidOperationException($"Error in script: {firstScriptError}");
        }

        _analyzer.Analyze(_analysisContext);

        var reportedIssues = _analysisContext.IssueReporter.Issues;
        reportedIssues.Should().BeEquivalentTo(ExpectedIssues);
    }
}
