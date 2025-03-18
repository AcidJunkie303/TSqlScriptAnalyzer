using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Models;

namespace DatabaseAnalyzer.Common.Tests.Fakes;

internal sealed class FakeIssueReporter : IIssueReporter
{
    public List<IIssue> Issues { get; } = [];

    IReadOnlyList<IIssue> IIssueReporter.Issues => Issues;

    public void Report(IDiagnosticDefinition rule, string databaseName, string relativeScriptFilePath, string? fullObjectName, CodeRegion codeRegion, params object[] insertionStrings)
    {
        var issue = Issue.Create(rule, databaseName, relativeScriptFilePath, fullObjectName, codeRegion, insertionStrings);
        Issues.Add(issue);
    }
}
