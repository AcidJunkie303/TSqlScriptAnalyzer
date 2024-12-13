using System.Collections.Concurrent;
using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

namespace DatabaseAnalyzer.Core;

internal sealed class IssueReporter : IIssueReporter
{
    private readonly ConcurrentBag<IIssue> _issues = [];

    public IReadOnlyList<IIssue> GetIssues() => _issues.ToImmutableArray();

    public void Report(IDiagnosticDefinition rule, string relativeScriptFilePath, string? fullObjectName, CodeRegion codeRegion, params object[] insertionStrings)
    {
        var issue = Issue.Create(rule, relativeScriptFilePath, fullObjectName, codeRegion, insertionStrings);
        _issues.Add(issue);
    }
}
