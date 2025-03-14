using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Core.Models;

public sealed record SuppressedIssue(
    IIssue Issue,
    string Reason
);
