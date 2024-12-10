using System.Collections.Concurrent;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

namespace DatabaseAnalyzer.Core;

internal sealed class IssueReporter : IIssueReporter
{
    private readonly ConcurrentBag<IIssue> _reportedIssues = [];

    public IReadOnlyList<IIssue> GetReportedIssues() => [.. _reportedIssues];

    public void Report(IDiagnosticDefinition rule, string fullScriptFilePath, string? fullObjectName, CodeRegion codeRegion, params object[] insertionStrings)
    {
        var issue = Issue.Create(rule, fullScriptFilePath, fullObjectName, codeRegion, insertionStrings);
        _reportedIssues.Add(issue);
    }
}
