using System.Collections.Concurrent;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Testing.Models;
using DatabaseAnalyzer.Testing.Services;

namespace DatabaseAnalyzer.Testing;

internal sealed class IssueReporter : IIssueReporter
{
    private readonly ConcurrentBag<IIssue> _reportedIssues = [];

    public IReadOnlyList<IIssue> GetReportedIssues() => [.. _reportedIssues];

    public void Report(IDiagnosticDefinition rule, string fullScriptFilePath, string? fullObjectName, SourceSpan codeRegion, params IReadOnlyList<string> insertionStrings)
    {
        AssertCorrectInsertionStringCount(rule.MessageTemplate, insertionStrings);

        var issue = Issue.Create(rule, fullScriptFilePath, fullObjectName, codeRegion, insertionStrings);
        _reportedIssues.Add(issue);

        static void AssertCorrectInsertionStringCount(string messageTemplate, IReadOnlyList<string> messageInsertionStrings)
        {
            var expectedInsertionStringCount = InsertionStringHelpers.CountInsertionStrings(messageTemplate);
            if (expectedInsertionStringCount == messageInsertionStrings.Count)
            {
                return;
            }

            throw new ArgumentException($"Expected {expectedInsertionStringCount} insertion strings, but got {messageInsertionStrings.Count}.", nameof(messageInsertionStrings));
        }
    }
}
