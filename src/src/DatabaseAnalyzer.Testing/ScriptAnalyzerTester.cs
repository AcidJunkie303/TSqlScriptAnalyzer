using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;
using FluentAssertions;
using Microsoft.SqlServer.Management.Dmf;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("maintainability", "CA1515:Consider making public types internal", Justification = "False positive. It is used in the DatabaseAnalyzers.DefaultAnalyzers.Tests project")]
public sealed class ScriptAnalyzerTester
{
    private readonly IAnalysisContext _analysisContext;
    private readonly IScriptAnalyzer _analyzer;

    public ScriptAnalyzerTester(
        IAnalysisContext analysisContext,
        IScriptAnalyzer analyzer,
        ScriptModel mainScript,
        IReadOnlyList<IIssue> expectedIssues)
    {
        MainScript = mainScript;
        ExpectedIssues = expectedIssues;
        _analysisContext = analysisContext;
        _analyzer = analyzer;
    }

    public ScriptModel MainScript { get; }
    public IReadOnlyList<IIssue> ExpectedIssues { get; }

    public void Test()
    {
        var firstScriptError = _analysisContext.Scripts.SelectMany(script => script.Errors).FirstOrDefault();
        if (firstScriptError is not null)
        {
            throw new InvalidOperandException($"Error in script. {firstScriptError}", firstScriptError);
        }

        _analyzer.AnalyzeScript(_analysisContext, MainScript);

        var reportedIssues = _analysisContext.IssueReporter.GetIssues();
        reportedIssues.Should().BeEquivalentTo(ExpectedIssues);
    }
}
