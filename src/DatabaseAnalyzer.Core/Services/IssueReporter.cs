using System.Collections.Concurrent;
using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Models;

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
