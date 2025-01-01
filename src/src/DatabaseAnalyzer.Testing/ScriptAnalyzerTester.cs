using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using FluentAssertions;
using Xunit.Abstractions;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("maintainability", "CA1515:Consider making public types internal", Justification = "False positive. It is used in the DatabaseAnalyzers.DefaultAnalyzers.Tests project")]
public sealed class ScriptAnalyzerTester
{
    private readonly IAnalysisContext _analysisContext;
    private readonly IScriptAnalyzer _analyzer;
    private readonly ITestOutputHelper? _testOutputHelper;

    public ScriptAnalyzerTester(
        IAnalysisContext analysisContext,
        IScriptAnalyzer analyzer,
        IScriptModel mainScript,
        IReadOnlyList<IIssue> expectedIssues,
        ITestOutputHelper? testOutputHelper)
    {
        _analysisContext = analysisContext;
        _analyzer = analyzer;
        _testOutputHelper = testOutputHelper;
        MainScript = mainScript;
        ExpectedIssues = expectedIssues;
    }

    public IScriptModel MainScript { get; }
    public IReadOnlyList<IIssue> ExpectedIssues { get; }

    public void Test()
    {
        var firstScriptError = _analysisContext.Scripts.SelectMany(static script => script.Errors).FirstOrDefault();
        if (firstScriptError is not null)
        {
            throw new InvalidOperationException($"Error in script: {firstScriptError}");
        }

        _analyzer.AnalyzeScript(_analysisContext, MainScript);

        var reportedIssues = _analysisContext.IssueReporter.Issues;
        WriteIssues(reportedIssues);

        // sometimes the order is not the same, therefore we cannot use BeEquivalentTo()
        reportedIssues.Should().HaveCount(ExpectedIssues.Count);
        foreach (var expectedIssue in ExpectedIssues)
        {
            reportedIssues.Should().ContainEquivalentOf(expectedIssue);
        }
    }

    private void WriteIssues(IReadOnlyList<IIssue> issues)
    {
        if (_testOutputHelper is null)
        {
            return;
        }

        if (issues.Count == 0)
        {
            _testOutputHelper.WriteLine("No issues reported");
            return;
        }

        _testOutputHelper.WriteLine($"{issues.Count} issue{(issues.Count == 1 ? "" : "s")} reported:");
        foreach (var issue in issues)
        {
            var insertionStrings = issue.MessageInsertions.Count == 0
                ? "<none>"
                : issue.MessageInsertions.StringJoin("░");
            var message = $"""{issue.DiagnosticDefinition.DiagnosticId}    CodeRegion="{issue.CodeRegion}"    Insertions="{insertionStrings}""";
            _testOutputHelper.WriteLine(message);
        }
    }
}
