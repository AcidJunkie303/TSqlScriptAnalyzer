using System.Collections.Concurrent;
using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

namespace DatabaseAnalyzer.Core.Services;

internal sealed class IssueReporter : IIssueReporter
{
    private readonly ConcurrentBag<IIssue> _issues = [];

    public IReadOnlyList<IIssue> Issues => _issues.ToImmutableArray();

    public void Report(IDiagnosticDefinition rule, string databaseName, string relativeScriptFilePath, string? fullObjectName, CodeRegion codeRegion, params object[] insertionStrings)
    {
        var issue = Issue.Create(rule, databaseName, relativeScriptFilePath, fullObjectName, codeRegion, insertionStrings);
        _issues.Add(issue);
    }
}
