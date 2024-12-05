namespace DatabaseAnalyzer.Core;

public sealed record ProgressCallbackArgs(
    bool IsBeginOfAction,
    string MessageTemplate,
    IReadOnlyList<string> InsertionStrings
);
